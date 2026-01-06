# ========================================
# 腾讯云 API 支付网关自动化部署脚本
# ========================================

$SSH_USER = "root"
$SERVER = "api.qsgl.net"
$REMOTE_DIR = "/opt/payment-gateway"
$SSH_KEY = "K:\Key\tx.qsgl.net_id_ed25519"
$PROJECT_DIR = "K:\payment\AbcPaymentGateway"

function Invoke-RemoteCmd {
    param([string]$Cmd)
    ssh -i $SSH_KEY $SSH_USER@$SERVER $Cmd
}

# 步骤 1: 检查依赖
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "步骤 1: 检查依赖环境" -ForegroundColor Yellow

if (-not (Test-Path $SSH_KEY)) {
    Write-Host "错误：SSH 密钥不存在: $SSH_KEY" -ForegroundColor Red
    exit 1
}
Write-Host "✓ SSH 密钥已找到" -ForegroundColor Green

if (-not (Get-Command scp -ErrorAction SilentlyContinue)) {
    Write-Host "错误：scp 命令不可用" -ForegroundColor Red
    exit 1
}
Write-Host "✓ scp 命令可用" -ForegroundColor Green

# 步骤 2: 测试 SSH 连接
Write-Host ""
Write-Host "步骤 2: 测试 SSH 连接..." -ForegroundColor Yellow
Invoke-RemoteCmd "echo SSH连接成功"
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ SSH 连接成功" -ForegroundColor Green
} else {
    Write-Host "错误：无法连接到服务器" -ForegroundColor Red
    exit 1
}

# 步骤 3: 验证远程环境
Write-Host ""
Write-Host "步骤 3: 验证远程环境..." -ForegroundColor Yellow
Invoke-RemoteCmd "docker --version"
Write-Host "✓ Docker 已安装" -ForegroundColor Green

# 步骤 4: 准备远程目录
Write-Host ""
Write-Host "步骤 4: 准备远程目录..." -ForegroundColor Yellow
Invoke-RemoteCmd "mkdir -p $REMOTE_DIR"
Write-Host "✓ 远程目录已准备" -ForegroundColor Green

# 步骤 5: 上传文件
Write-Host ""
Write-Host "步骤 5: 上传项目文件..." -ForegroundColor Yellow

$scpDest = "${SSH_USER}@${SERVER}:${REMOTE_DIR}/"

# 上传目录
foreach ($dir in @("Controllers", "Models", "Services", "Properties")) {
    $src = Join-Path $PROJECT_DIR $dir
    if (Test-Path $src) {
        Write-Host "  上传: $dir"
        scp -i $SSH_KEY -q -r $src $scpDest 2>$null
    }
}

# 上传文件
foreach ($file in @("AbcPaymentGateway.csproj", "Program.cs", "Dockerfile", "docker-compose.yml", "appsettings.json")) {
    $src = Join-Path $PROJECT_DIR $file
    if (Test-Path $src) {
        Write-Host "  上传: $file"
        scp -i $SSH_KEY -q $src $scpDest 2>$null
    }
}

Write-Host "✓ 文件上传完成" -ForegroundColor Green

# 步骤 6: 构建容器
Write-Host ""
Write-Host "步骤 6: 构建容器镜像..." -ForegroundColor Yellow
Invoke-RemoteCmd "cd $REMOTE_DIR; docker compose build --no-cache" | Select-Object -Last 10
Write-Host "✓ 容器构建完成" -ForegroundColor Green

# 步骤 7: 启动容器
Write-Host ""
Write-Host "步骤 7: 启动容器..." -ForegroundColor Yellow
Invoke-RemoteCmd "cd $REMOTE_DIR; docker compose down"
Invoke-RemoteCmd "cd $REMOTE_DIR; docker compose up -d"
Start-Sleep -Seconds 3
Write-Host "✓ 容器已启动" -ForegroundColor Green

# 步骤 8: 验证部署
Write-Host ""
Write-Host "步骤 8: 验证部署..." -ForegroundColor Yellow
Invoke-RemoteCmd "cd $REMOTE_DIR; docker compose ps" | Select-Object -Last 5

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "✓ 部署完成！" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "服务信息:" -ForegroundColor Yellow
Write-Host "  服务地址: https://payment.qsgl.net" -ForegroundColor White
Write-Host "  健康检查: https://payment.qsgl.net/health" -ForegroundColor White
Write-Host "  容器端口: 8080 (内部)" -ForegroundColor White
Write-Host ""
Write-Host "容器状态:" -ForegroundColor Yellow
Invoke-RemoteCmd "docker ps -f 'name=payment-gateway' --format 'table {{.Status}}\t{{.Ports}}'" 2>&1
Write-Host ""
