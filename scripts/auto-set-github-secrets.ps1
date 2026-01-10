# PowerShell Script: auto-set-github-secrets.ps1
# Fully automated GitHub Secrets configuration

param(
    [string]$GitHubRepo = "qsswgl/AbcPaymentGateway",
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    [string]$ReadmePath = "k:\payment\AbcPaymentGateway\README.md"
)

Write-Host "==== GitHub Secrets Auto-Configuration ====" -ForegroundColor Cyan

# Step 1: Read secrets from README
Write-Host "[1/4] Reading secrets from README.md..." -ForegroundColor Yellow
$Secrets = @{}
$InSecretsBlock = $false

Get-Content $ReadmePath | ForEach-Object {
    if ($_ -match "````secrets") {
        $InSecretsBlock = -not $InSecretsBlock
        return
    }
    if ($InSecretsBlock -and ($_ -match "^([A-Z_]+):\s*(.+)$")) {
        $KeyName = $matches[1]
        $ValueText = $matches[2].Trim()
        
        if ($ValueText -like "#*" -or $ValueText -eq "" -or $ValueText -like "<*>") {
            return
        }
        
        if ($ValueText -like "file:*") {
            $FilePath = $ValueText.Substring(5)
            if (Test-Path $FilePath) {
                $Secrets[$KeyName] = Get-Content $FilePath -Raw
            }
        } else {
            $Secrets[$KeyName] = $ValueText
        }
    }
}

Write-Host "Found $($Secrets.Count) secrets" -ForegroundColor Green

# Step 2: Get repository public key
Write-Host "[2/4] Getting repository public key..." -ForegroundColor Yellow
$Url = "https://api.github.com/repos/$GitHubRepo/actions/secrets/public-key"
$Headers = @{
    Authorization = "Bearer $GitHubToken"
    Accept = "application/vnd.github+json"
}

$PublicKeyResponse = Invoke-RestMethod -Uri $Url -Headers $Headers -Method GET
$key_id = $PublicKeyResponse.key_id
$public_key = $PublicKeyResponse.key
Write-Host "Got public key (ID: $key_id)" -ForegroundColor Green

# Step 3: Encrypt secrets using Python + PyNaCl
Write-Host "[3/4] Encrypting secrets..." -ForegroundColor Yellow

$PythonScript = @"
import sys
import base64
import json
from nacl import encoding, public

def encrypt_secret(public_key_b64, secret_value):
    public_key = public.PublicKey(public_key_b64.encode("utf-8"), encoding.Base64Encoder())
    sealed_box = public.SealedBox(public_key)
    encrypted = sealed_box.encrypt(secret_value.encode("utf-8"))
    return base64.b64encode(encrypted).decode("utf-8")

secrets_dict = json.loads(sys.argv[1])
public_key = sys.argv[2]
result = {}

for key, value in secrets_dict.items():
    if isinstance(value, str):
        result[key] = encrypt_secret(public_key, value)
    else:
        result[key] = encrypt_secret(public_key, str(value))

print(json.dumps(result))
"@

$SecretsJson = $Secrets | ConvertTo-Json -Compress
$EncryptedJson = python -c $PythonScript $SecretsJson $public_key
$EncryptedSecrets = $EncryptedJson | ConvertFrom-Json

Write-Host "Encrypted all secrets" -ForegroundColor Green

# Step 4: Upload secrets to GitHub
Write-Host "[4/4] Uploading secrets to GitHub..." -ForegroundColor Yellow
$UploadCount = 0

foreach ($key in $Secrets.Keys) {
    $Url = "https://api.github.com/repos/$GitHubRepo/actions/secrets/$key"
    $Body = @{
        encrypted_value = $EncryptedSecrets.$key
        key_id = $key_id
    } | ConvertTo-Json

    try {
        Invoke-RestMethod -Uri $Url -Headers $Headers -Method PUT -Body $Body -ContentType "application/json" | Out-Null
        Write-Host "  ✓ $key" -ForegroundColor Green
        $UploadCount++
    } catch {
        Write-Host "  ✗ $key - $_" -ForegroundColor Red
    }
}

Write-Host "`nSUCCESS: $UploadCount/$($Secrets.Count) secrets configured" -ForegroundColor Cyan
