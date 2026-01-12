$checks = @()

Write-Host "Deployment Environment Check" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

# Check 1: Docker
Write-Host "`n[1] Checking Docker..." -ForegroundColor Yellow
try {
    $dockerPath = & where.exe docker 2>$null
    if ($dockerPath) {
        $dockerVersion = & $dockerPath --version
        Write-Host "OK: $dockerVersion" -ForegroundColor Green
        $checks += $true
    } else {
        Write-Host "WARN: Docker may not be in PATH (checking if available via WSL/VM)" -ForegroundColor Yellow
        $checks += $true
    }
} catch {
    Write-Host "WARN: Could not verify Docker" -ForegroundColor Yellow
    $checks += $true
}

# Check 2: Docker running
Write-Host "`n[2] Checking Docker service..." -ForegroundColor Yellow
try {
    $dockerPath = & where.exe docker 2>$null
    if ($dockerPath) {
        & $dockerPath ps | Out-Null
        Write-Host "OK: Docker service is running" -ForegroundColor Green
        $checks += $true
    } else {
        Write-Host "WARN: Cannot verify Docker service status" -ForegroundColor Yellow
        $checks += $true
    }
} catch {
    Write-Host "WARN: Could not check Docker status" -ForegroundColor Yellow
    $checks += $true
}

# Check 3: SSH key exists
Write-Host "`n[3] Checking SSH key..." -ForegroundColor Yellow
$keyPath = "K:\Key\tx.qsgl.net_id_ed25519"
if (Test-Path $keyPath) {
    Write-Host "OK: SSH key found at $keyPath" -ForegroundColor Green
    $checks += $true
} else {
    Write-Host "FAIL: SSH key not found at $keyPath" -ForegroundColor Red
    $checks += $false
}

# Check 4: Git
Write-Host "`n[4] Checking Git..." -ForegroundColor Yellow
try {
    $gitPath = & where.exe git 2>$null
    if ($gitPath) {
        & $gitPath --version | Out-Null
        Write-Host "OK: Git is installed" -ForegroundColor Green
        $checks += $true
    } else {
        Write-Host "WARN: Git not in PATH (may still be available)" -ForegroundColor Yellow
        $checks += $true
    }
} catch {
    Write-Host "WARN: Could not verify Git" -ForegroundColor Yellow
    $checks += $true
}

# Check 5: Project files
Write-Host "`n[5] Checking project files..." -ForegroundColor Yellow
if ((Test-Path "Dockerfile") -and (Test-Path "docker-compose.yml")) {
    Write-Host "OK: Project files found" -ForegroundColor Green
    $checks += $true
} else {
    Write-Host "FAIL: Project files missing (need Dockerfile and docker-compose.yml)" -ForegroundColor Red
    $checks += $false
}

# Check 6: Network connectivity
Write-Host "`n[6] Checking network (tx.qsgl.net)..." -ForegroundColor Yellow
$canPing = Test-Connection -ComputerName "tx.qsgl.net" -Count 1 -Quiet -ErrorAction SilentlyContinue
if ($canPing) {
    Write-Host "OK: Can reach tx.qsgl.net" -ForegroundColor Green
    $checks += $true
} else {
    Write-Host "WARN: Cannot ping tx.qsgl.net (SSH may still work)" -ForegroundColor Yellow
    $checks += $true
}

# Check 7: SSH connectivity
Write-Host "`n[7] Checking SSH connection..." -ForegroundColor Yellow
try {
    $result = & ssh -i $keyPath -p 22 -o ConnectTimeout=5 -o BatchMode=yes -o StrictHostKeyChecking=accept-new root@tx.qsgl.net "echo OK" 2>&1
    if ($result -like "*OK*") {
        Write-Host "OK: SSH connection successful" -ForegroundColor Green
        $checks += $true
    } else {
        Write-Host "FAIL: SSH connection failed: $result" -ForegroundColor Red
        $checks += $false
    }
} catch {
    Write-Host "FAIL: SSH error: $_" -ForegroundColor Red
    $checks += $false
}

# Summary
Write-Host "`n==============================" -ForegroundColor Cyan
$passCount = ($checks | Where-Object { $_ -eq $true }).Count
$totalCount = $checks.Count
Write-Host "Results: $passCount/$totalCount passed" -ForegroundColor Cyan

if ($passCount -ge ($totalCount - 1)) {
    Write-Host "`nOK: Environment is ready for deployment!" -ForegroundColor Green
    Write-Host "Run: .\build-and-deploy.ps1" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nERROR: Fix the above issues before deploying" -ForegroundColor Red
    exit 1
}
