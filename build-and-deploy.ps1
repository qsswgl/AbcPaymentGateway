# Payment Gateway Local Build and Remote Deploy Script
# Build Docker image locally -> Package -> Upload via SSH/SCP -> Deploy on Tencent Cloud

param(
    [string]$RemoteHost = "tx.qsgl.net",
    [string]$RemoteUser = "root",
    [int]$RemotePort = 22,
    [string]$RemoteDir = "/opt/payment-gateway",
    [string]$SSHKeyPath = "K:\Key\tx.qsgl.net_id_ed25519",
    [string]$ImageName = "payment-gateway-jit",
    [string]$ImageTag = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Payment Gateway - Local Build & Remote Deploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Check prerequisites
Write-Host "`n[1/5] Checking prerequisites..." -ForegroundColor Yellow

# Find Docker executable
$DockerCmd = $null
$DockerPaths = @(
    "C:\Program Files\Docker\Docker\resources\bin\docker.exe",
    "C:\ProgramData\DockerDesktop\version-bin\docker.exe",
    "$env:ProgramFiles\Docker\Docker\resources\bin\docker.exe"
)

foreach ($path in $DockerPaths) {
    if (Test-Path $path) {
        $DockerCmd = $path
        break
    }
}

if (-not $DockerCmd) {
    # Try to find in PATH or common locations
    $found = Get-Command docker -ErrorAction SilentlyContinue
    if ($found) {
        $DockerCmd = "docker"
    } else {
        Write-Host "ERROR: Docker not found. Please ensure Docker Desktop is installed and running" -ForegroundColor Red
        exit 1
    }
}

Write-Host "OK: Docker found at: $DockerCmd" -ForegroundColor Green

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

# Step 2: Build Docker image locally
Write-Host "`n[2/5] Building Docker image ($ImageName)..." -ForegroundColor Yellow
Write-Host "Command: $DockerCmd build -t $($ImageName):$ImageTag ." -ForegroundColor Gray

try {
    & $DockerCmd build -t "$ImageName`:$ImageTag" .
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed"
    }
    Write-Host "OK: Docker image built successfully" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker build failed - $_" -ForegroundColor Red
    Write-Host "Make sure Docker Desktop is running" -ForegroundColor Yellow
    exit 1
}

# Step 3: Export image as TAR file
Write-Host "`n[3/5] Exporting image as TAR file..." -ForegroundColor Yellow
$TarFile = "payment-gateway-$ImageTag.tar.gz"
Write-Host "Command: $DockerCmd save $ImageName`:$ImageTag | gzip > $TarFile" -ForegroundColor Gray

try {
    & $DockerCmd save "$ImageName`:$ImageTag" | & gzip > $TarFile
    if ($LASTEXITCODE -ne 0) {
        throw "Image export failed"
    }
    
    $TarSize = (Get-Item $TarFile).Length / 1MB
    Write-Host "OK: Image exported - $TarFile (Size: $([math]::Round($TarSize, 2)) MB)" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Image export failed - $_" -ForegroundColor Red
    exit 1
}

# Step 4: Upload image to remote server via SCP
Write-Host "`n[4/5] Uploading image to remote server ($RemoteHost)..." -ForegroundColor Yellow

$SCPCommand = "scp -i `"$SSHKeyPath`" -P $RemotePort `"$TarFile`" `"${RemoteUser}@${RemoteHost}:/tmp/`""
Write-Host "Command: $SCPCommand" -ForegroundColor Gray

try {
    Invoke-Expression $SCPCommand
    if ($LASTEXITCODE -ne 0) {
        throw "SCP upload failed"
    }
    Write-Host "OK: Image uploaded to /tmp/$TarFile" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Upload failed - $_" -ForegroundColor Red
    Write-Host "Check SSH key and network connection" -ForegroundColor Yellow
    exit 1
}

# Step 5: Execute remote deployment script
Write-Host "`n[5/5] Deploying on remote server..." -ForegroundColor Yellow

$RemoteScript = @"
#!/bin/bash
set -e
echo "=== Starting remote deployment ==="
cd $RemoteDir

echo "Step 1: Loading new image..."
docker load < /tmp/$TarFile

echo "Step 2: Stopping old containers..."
docker-compose down || true

echo "Step 3: Starting containers with new image..."
docker-compose up -d

echo "Step 4: Waiting for service to start..."
sleep 5

echo "Step 5: Health check..."
curl -fsS http://localhost:8080/health && echo "OK: Health check passed" || (echo "ERROR: Health check failed"; exit 1)

echo "Step 6: Cleaning up temporary files..."
rm /tmp/$TarFile

echo "=== Deployment completed successfully! ==="
docker ps | grep payment-gateway
"@

try {
    $RemoteScript | & ssh -i $SSHKeyPath -p $RemotePort "${RemoteUser}@${RemoteHost}" 'bash -s'
    if ($LASTEXITCODE -ne 0) {
        throw "Remote deployment failed"
    }
} catch {
    Write-Host "ERROR: Remote deployment failed - $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Service URL: https://payment.qsgl.net" -ForegroundColor Cyan
Write-Host "API Docs: https://payment.qsgl.net/swagger" -ForegroundColor Cyan

# Cleanup local temporary file
Write-Host "`nCleaning up local temporary file..." -ForegroundColor Yellow
Remove-Item $TarFile -Force
Write-Host "OK: Temporary file deleted" -ForegroundColor Green

Write-Host "`nDeployment finished!" -ForegroundColor Green
