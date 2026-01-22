# Remote Build and Deploy Script
# Upload source code -> Build on remote server -> Deploy

param(
    [string]$RemoteHost = "tx.qsgl.net",
    [string]$RemoteUser = "root",
    [int]$RemotePort = 22,
    [string]$RemoteDir = "/opt/payment-gateway",
    [string]$SSHKeyPath = "K:\Key\tx.qsgl.net_id_ed25519"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Payment Gateway - Remote Build & Deploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Check prerequisites
Write-Host "`n[1/5] Checking prerequisites..." -ForegroundColor Yellow

if (-not (Test-Path $SSHKeyPath)) {
    Write-Host "ERROR: SSH key not found at: $SSHKeyPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "Dockerfile")) {
    Write-Host "ERROR: Not in project root directory (missing Dockerfile)" -ForegroundColor Red
    exit 1
}

Write-Host "OK: SSH key found" -ForegroundColor Green
Write-Host "OK: Project directory confirmed" -ForegroundColor Green

# Step 2: Create temporary archive
Write-Host "`n[2/5] Creating source code archive..." -ForegroundColor Yellow
$TempDir = [System.IO.Path]::GetTempPath()
$ArchiveFile = Join-Path $TempDir "payment-gateway-src.tar.gz"

try {
    # Remove old archive if exists
    if (Test-Path $ArchiveFile) {
        Remove-Item $ArchiveFile -Force
    }

    # Create tar.gz archive (exclude unnecessary files)
    $ExcludePatterns = @(
        "bin", "obj", ".git", ".vs", ".vscode", 
        "*.tar.gz", "*.log", "logs/*", 
        "build-and-deploy.ps1", "check-deploy-env.ps1"
    )
    
    Write-Host "Creating archive: $ArchiveFile" -ForegroundColor Gray
    
    # Use tar command (available in Windows 10+)
    $excludeArgs = $ExcludePatterns | ForEach-Object { "--exclude=$_" }
    & tar -czf $ArchiveFile $excludeArgs -C . .
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create archive"
    }
    
    $sizeInMB = [math]::Round((Get-Item $ArchiveFile).Length / 1MB, 2)
    Write-Host "OK: Archive created ($sizeInMB MB)" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Failed to create archive - $_" -ForegroundColor Red
    exit 1
}

# Step 3: Upload to remote server
Write-Host "`n[3/5] Uploading source code to remote server..." -ForegroundColor Yellow
$RemoteTempFile = "/tmp/payment-gateway-src.tar.gz"

try {
    Write-Host "Command: scp -i `"$SSHKeyPath`" -P $RemotePort $ArchiveFile ${RemoteUser}@${RemoteHost}:$RemoteTempFile" -ForegroundColor Gray
    & scp -i $SSHKeyPath -P $RemotePort $ArchiveFile "${RemoteUser}@${RemoteHost}:$RemoteTempFile"
    
    if ($LASTEXITCODE -ne 0) {
        throw "SCP upload failed"
    }
    Write-Host "OK: Source code uploaded" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Upload failed - $_" -ForegroundColor Red
    exit 1
}

# Step 4: Build and deploy on remote server
Write-Host "`n[4/5] Building and deploying on remote server..." -ForegroundColor Yellow
Write-Host "This may take 3-5 minutes for the build process..." -ForegroundColor Gray

$RemoteScript = @"
#!/bin/bash
set -e

echo "Stopping existing containers..."
cd $RemoteDir
docker-compose down || true

echo "Extracting source code..."
rm -rf /tmp/payment-gateway-build
mkdir -p /tmp/payment-gateway-build
tar -xzf $RemoteTempFile -C /tmp/payment-gateway-build

echo "Updating docker-compose.yml..."
cp /tmp/payment-gateway-build/docker-compose.yml $RemoteDir/docker-compose.yml

echo "Building Docker image..."
cd /tmp/payment-gateway-build
docker build -t payment-gateway-jit:latest .

echo "Cleaning up build files..."
cd $RemoteDir
rm -rf /tmp/payment-gateway-build
rm -f $RemoteTempFile

echo "Starting containers..."
docker-compose up -d

echo "Waiting for service to start..."
sleep 5

echo "Checking health..."
if curl -fsS http://localhost:8080/health > /dev/null 2>&1; then
    echo "Health check passed!"
else
    echo "Warning: Health check endpoint not responding (might need more time)"
fi

echo "Container status:"
docker-compose ps

echo "Recent logs:"
docker-compose logs --tail=20
"@

try {
    # Save script to temp file with Unix line endings
    $RemoteScriptFile = Join-Path $TempDir "deploy-script.sh"
    
    # Write with Unix line endings (LF only, no CRLF)
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($RemoteScriptFile, $RemoteScript.Replace("`r`n", "`n"), $utf8NoBom)
    
    # Execute remote script via SSH
    Write-Host "Executing remote deployment script..." -ForegroundColor Gray
    Get-Content $RemoteScriptFile -Raw | & ssh -i $SSHKeyPath -p $RemotePort "${RemoteUser}@${RemoteHost}" "bash -s"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Remote deployment failed"
    }
    
    Write-Host "OK: Remote build and deployment completed" -ForegroundColor Green
    
    # Cleanup local temp script
    Remove-Item $RemoteScriptFile -Force
} catch {
    Write-Host "ERROR: Remote deployment failed - $_" -ForegroundColor Red
    exit 1
}

# Step 5: Verify deployment
Write-Host "`n[5/5] Verifying deployment..." -ForegroundColor Yellow

try {
    Write-Host "Testing health endpoint..." -ForegroundColor Gray
    & ssh -i $SSHKeyPath -p $RemotePort "${RemoteUser}@${RemoteHost}" "curl -fsS http://localhost:8080/health"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK: Health check passed" -ForegroundColor Green
    } else {
        Write-Host "WARN: Health check failed (service might still be starting)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "WARN: Could not verify health - $_" -ForegroundColor Yellow
}

# Cleanup local archive
if (Test-Path $ArchiveFile) {
    Remove-Item $ArchiveFile -Force
    Write-Host "OK: Local temporary files cleaned up" -ForegroundColor Green
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nService URL: https://payment.qsgl.net" -ForegroundColor Cyan
Write-Host "Swagger UI: https://payment.qsgl.net/swagger" -ForegroundColor Cyan
Write-Host "`nTo check logs: ssh -i `"$SSHKeyPath`" ${RemoteUser}@${RemoteHost} 'cd $RemoteDir && docker-compose logs -f'" -ForegroundColor Gray
