# Native AOT 自动化部署脚本 (简化版)
# 用法: .\deploy-simplified.ps1
# 功能：在本地构建 Native AOT 镜像，上传到远程服务器并部署

param(
    [string]$SSH_KEY = "K:\Key\tx.qsgl.net_id_ed25519",
    [string]$SSH_USER = "root",
    [string]$SERVER = "api.qsgl.net",
    [string]$REMOTE_DIR = "/opt/payment-gateway",
    [string]$CONTAINER_LABEL = "payment",
    [string]$DOMAIN = "payment.qsgl.net"
)

# 颜色输出辅助
function Write-Success { Write-Host "✓ $args" -ForegroundColor Green }
function Write-Error-Custom { Write-Host "✗ $args" -ForegroundColor Red; exit 1 }
function Write-Info { Write-Host "→ $args" -ForegroundColor Yellow }

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Native AOT 自动化部署脚本" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# ============= 步骤 1: 检查前置条件 =============
Write-Info "检查 SSH 密钥..."
if (!(Test-Path $SSH_KEY)) {
    Write-Error-Custom "SSH 密钥不存在: $SSH_KEY"
}
Write-Success "SSH 密钥存在"

Write-Info "测试 SSH 连接..."
ssh -i $SSH_KEY -o StrictHostKeyChecking=no -o ConnectTimeout=10 "$SSH_USER@$SERVER" "echo 'SSH连接成功'" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "SSH 连接失败"
}
Write-Success "SSH 连接成功"

# ============= 步骤 2: 清理远程目录 =============
Write-Info "清理远程旧数据..."
ssh -i $SSH_KEY "$SSH_USER@$SERVER" "rm -rf $REMOTE_DIR && mkdir -p $REMOTE_DIR" 2>$null
Write-Success "远程目录已清理"

# ============= 步骤 3: 上传项目文件 =============
Write-Info "上传项目文件到 $SERVER:$REMOTE_DIR..."
# 使用 tar 通过 SSH 管道直接上传，避免路径问题
$filesToUpload = @(
    "*.csproj",
    "*.cs",
    "Dockerfile",
    "docker-compose.yml",
    ".dockerignore",
    "appsettings.json",
    "appsettings.Development.json",
    "*.md",
    "Controllers",
    "Models",
    "Services",
    "Properties",
    "bin/Release",
    "obj"
)

# 使用 tar 压缩后通过 SSH 上传，在远端解压
$excludePatterns = @("bin/Debug", "obj", ".git", ".vs", ".vscode")
$tarCmd = "tar --exclude='.git' --exclude='.vs' --exclude='.vscode' --exclude='bin/Debug' --exclude='obj' -czf - "
foreach ($pattern in $filesToUpload) {
    $tarCmd += "$pattern "
}
$tarCmd += "| ssh -i $SSH_KEY $SSH_USER@$SERVER 'cd $REMOTE_DIR && tar -xzf -'"

# 执行 tar 上传（适用于 Windows 10 21H2+ 或 Git Bash）
Write-Info "使用 tar 压缩上传..."
& cmd /c "cd /d K:\payment\AbcPaymentGateway && $tarCmd"
if ($LASTEXITCODE -ne 0) {
    Write-Info "tar 上传失败，尝试 scp 上传..."
    # 备选：使用 scp 递归上传关键目录
    scp -i $SSH_KEY -r Controllers "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
    scp -i $SSH_KEY -r Models "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
    scp -i $SSH_KEY -r Services "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
    scp -i $SSH_KEY *.csproj "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
    scp -i $SSH_KEY *.cs "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
    scp -i $SSH_KEY Dockerfile "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
    scp -i $SSH_KEY docker-compose.yml "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
    scp -i $SSH_KEY appsettings.json "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
    scp -i $SSH_KEY .dockerignore "$SSH_USER@$SERVER:$REMOTE_DIR/" 2>$null
}
Write-Success "项目文件上传完成"

# ============= 步骤 4: 远程构建和部署 =============
Write-Info "在远程服务器上构建并启动容器..."

$remoteScript = @"
set -e
cd $REMOTE_DIR

echo '→ 检查 Docker 环境...'
docker --version
docker compose version || docker-compose --version

echo '→ 创建 Traefik 网络（如果不存在）...'
docker network inspect traefik-network > /dev/null 2>&1 || docker network create traefik-network

echo '→ 停止旧容器...'
docker compose down 2>/dev/null || true

echo '→ 清理旧镜像...'
docker images | grep payment | awk '{print \$3}' | xargs -r docker rmi -f 2>/dev/null || true

echo '→ 构建 Native AOT 镜像（这可能需要 5-10 分钟）...'
docker compose build --no-cache

echo '→ 启动容器...'
docker compose up -d

echo '→ 等待容器启动...'
sleep 8

echo '→ 验证容器状态...'
docker ps | grep payment || (echo '容器启动失败'; docker logs payment-gateway 2>&1 | tail -20; exit 1)

echo '→ 测试健康检查（最多等待 60 秒）...'
for i in {1..30}; do
    if curl -f http://localhost:8080/api/payment/health 2>/dev/null; then
        echo '✓ 健康检查通过'
        break
    fi
    if [ \$i -eq 30 ]; then
        echo '✗ 健康检查失败，查看日志：'
        docker logs payment-gateway | tail -30
        exit 1
    fi
    echo "  等待服务启动... (\$i/30)"
    sleep 2
done

echo '→ 清理构建缓存...'
docker builder prune -f 2>/dev/null || true

echo ''
echo '========================================='
echo '✓ Native AOT 容器部署成功！'
echo '========================================='
echo ''
echo '容器信息:'
docker ps --filter 'name=payment' --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
echo ''
echo '服务地址:'
echo '  内部地址: http://localhost:8080'
echo '  外部地址: https://$DOMAIN'
echo '  健康检查: https://$DOMAIN/api/payment/health'
echo ''
"@

# 将 CRLF 转换为 LF，确保 Bash 脚本正确执行
$remoteScript = $remoteScript -replace "`r`n", "`n"

# 通过管道将脚本传递给 SSH 执行
$remoteScript | ssh -i $SSH_KEY "$SSH_USER@$SERVER" "bash -s"
if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "远程部署失败，请检查服务器日志"
}

Write-Success "部署完成！"

# ============= 步骤 5: 验证部署 =============
Write-Info "验证部署结果..."
ssh -i $SSH_KEY "$SSH_USER@$SERVER" "docker ps --filter 'name=payment' --format 'json'" 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Success "容器: $($_.Names) | 状态: $($_.Status)"
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "🎉 部署完成！" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📌 重要信息:" -ForegroundColor Yellow
Write-Host "  域名: https://$DOMAIN" -ForegroundColor Cyan
Write-Host "  容器标签: $CONTAINER_LABEL" -ForegroundColor Cyan
Write-Host "  SSH 地址: ssh -i '$SSH_KEY' $SSH_USER@$SERVER" -ForegroundColor Gray
Write-Host ""
Write-Host "📋 常用命令:" -ForegroundColor Yellow
Write-Host "  查看日志: ssh -i '$SSH_KEY' $SSH_USER@$SERVER 'docker logs -f payment-gateway'" -ForegroundColor Gray
Write-Host "  重启容器: ssh -i '$SSH_KEY' $SSH_USER@$SERVER 'cd $REMOTE_DIR && docker compose restart'" -ForegroundColor Gray
Write-Host "  查看状态: ssh -i '$SSH_KEY' $SSH_USER@$SERVER 'docker ps | grep payment'" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ 部署脚本执行完成！" -ForegroundColor Green
Write-Host ""
