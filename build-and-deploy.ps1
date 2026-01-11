# Payment Gateway 本地构建和远程部署脚本
# 功能：本地编译 Docker 镜像 → 打包 → 通过 SSH/SCP 上传到腾讯云 → 远程执行更新部署

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
Write-Host "Payment Gateway 本地构建与远程部署" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 步骤 1: 检查必要条件
Write-Host "`n[1/5] 检查必要条件..." -ForegroundColor Yellow
if (-not (Test-Path $SSHKeyPath)) {
    Write-Host "❌ SSH 私钥文件不存在: $SSHKeyPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "Dockerfile")) {
    Write-Host "❌ 当前目录不是项目根目录（缺少 Dockerfile）" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Docker 已安装" -ForegroundColor Green
Write-Host "✅ SSH 私钥已找到" -ForegroundColor Green

# 步骤 2: 本地构建 Docker 镜像
Write-Host "`n[2/5] 构建 Docker 镜像 ($ImageName)..." -ForegroundColor Yellow
Write-Host "命令: docker build -t $($ImageName):$ImageTag ." -ForegroundColor Gray

docker build -t "$ImageName`:$ImageTag" .
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker 构建失败" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker 镜像构建成功" -ForegroundColor Green

# 步骤 3: 保存镜像为 TAR 文件
Write-Host "`n[3/5] 导出镜像为 TAR 文件..." -ForegroundColor Yellow
$TarFile = "payment-gateway-$ImageTag.tar.gz"
Write-Host "命令: docker save $ImageName`:$ImageTag | gzip > $TarFile" -ForegroundColor Gray

docker save "$ImageName`:$ImageTag" | gzip > $TarFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 镜像导出失败" -ForegroundColor Red
    exit 1
}

$TarSize = (Get-Item $TarFile).Length / 1MB
Write-Host "✅ 镜像已导出: $TarFile (大小: $([math]::Round($TarSize, 2)) MB)" -ForegroundColor Green

# 步骤 4: 通过 SCP 上传镜像到远程服务器
Write-Host "`n[4/5] 上传镜像到远程服务器 ($RemoteHost)..." -ForegroundColor Yellow

$SCPCommand = "scp -i `"$SSHKeyPath`" -P $RemotePort `"$TarFile`" `"${RemoteUser}@${RemoteHost}:/tmp/`""
Write-Host "命令: $SCPCommand" -ForegroundColor Gray

Invoke-Expression $SCPCommand
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 上传失败 (请检查 SSH 密钥和网络连接)" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 镜像已上传到 /tmp/$TarFile" -ForegroundColor Green

# 步骤 5: 远程执行更新部署脚本
Write-Host "`n[5/5] 在远程服务器执行部署..." -ForegroundColor Yellow

$RemoteScript = @"
#!/bin/bash
set -e
echo "=== 开始远程部署 ==="
cd $RemoteDir

echo "步骤 1: 加载新镜像..."
docker load < /tmp/$TarFile

echo "步骤 2: 删除旧容器..."
docker-compose down || true

echo "步骤 3: 使用新镜像启动容器..."
docker-compose up -d

echo "步骤 4: 等待服务启动..."
sleep 5

echo "步骤 5: 健康检查..."
curl -fsS http://localhost:8080/health && echo "✅ 健康检查通过" || (echo "❌ 健康检查失败"; exit 1)

echo "步骤 6: 清理临时文件..."
rm /tmp/$TarFile

echo "=== ✅ 部署成功! ==="
docker ps | grep payment-gateway
"@

$SSHCommand = "ssh -i `"$SSHKeyPath`" -p $RemotePort ${RemoteUser}@${RemoteHost}"
Write-Host "命令: $SSHCommand << 'EOF'" -ForegroundColor Gray

$RemoteScript | & ssh -i $SSHKeyPath -p $RemotePort "${RemoteUser}@${RemoteHost}" 'bash -s'
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 远程部署执行失败" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ 部署完成!" -ForegroundColor Green
Write-Host "服务地址: https://payment.qsgl.net" -ForegroundColor Cyan

# 清理本地临时文件
Write-Host "`n清理本地临时文件..." -ForegroundColor Yellow
Remove-Item $TarFile -Force
Write-Host "✅ 临时文件已删除" -ForegroundColor Green
