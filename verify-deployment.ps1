# 部署验证脚本
# 在服务器上运行此脚本以验证部署是否成功

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "农行支付网关 - 部署验证" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$allPassed = $true

# 检查 Docker
Write-Host "检查 Docker..." -NoNewline
try {
    $dockerVersion = docker --version
    Write-Host " ✓" -ForegroundColor Green
    Write-Host "  $dockerVersion" -ForegroundColor Gray
} catch {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "  Docker 未安装或无法访问" -ForegroundColor Red
    $allPassed = $false
}

# 检查容器状态
Write-Host "检查容器状态..." -NoNewline
$containerStatus = docker ps --filter "name=payment" --format "{{.Status}}"
if ($containerStatus -match "Up") {
    Write-Host " ✓" -ForegroundColor Green
    Write-Host "  容器运行中: $containerStatus" -ForegroundColor Gray
} else {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "  容器未运行" -ForegroundColor Red
    $allPassed = $false
}

# 检查本地健康检查
Write-Host "测试本地健康检查..." -NoNewline
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/api/payment/health" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host " ✓" -ForegroundColor Green
        Write-Host "  响应: $($response.Content)" -ForegroundColor Gray
    } else {
        Write-Host " ✗" -ForegroundColor Red
        $allPassed = $false
    }
} catch {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "  错误: $($_.Exception.Message)" -ForegroundColor Red
    $allPassed = $false
}

# 检查外部访问
Write-Host "测试外部访问..." -NoNewline
try {
    $response = Invoke-WebRequest -Uri "https://payment.qsgl.net/api/payment/health" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host " ✓" -ForegroundColor Green
        Write-Host "  HTTPS 访问正常" -ForegroundColor Gray
    } else {
        Write-Host " ✗" -ForegroundColor Red
        $allPassed = $false
    }
} catch {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "  错误: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  提示: 可能需要等待 Traefik 配置生效或 DNS 解析" -ForegroundColor Yellow
    # 外部访问失败不影响整体验证
}

# 检查日志目录
Write-Host "检查日志目录..." -NoNewline
if (Test-Path "./logs") {
    Write-Host " ✓" -ForegroundColor Green
} else {
    Write-Host " ✗" -ForegroundColor Yellow
    Write-Host "  日志目录不存在，将自动创建" -ForegroundColor Yellow
}

# 检查证书目录
Write-Host "检查证书目录..." -NoNewline
if (Test-Path "./cert") {
    Write-Host " ✓" -ForegroundColor Green
    $certCount = (Get-ChildItem -Path "./cert" -Recurse -File).Count
    Write-Host "  找到 $certCount 个证书文件" -ForegroundColor Gray
} else {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "  证书目录不存在" -ForegroundColor Red
    $allPassed = $false
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
if ($allPassed) {
    Write-Host "验证通过！部署成功！" -ForegroundColor Green
} else {
    Write-Host "验证失败！请检查错误信息" -ForegroundColor Red
}
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# 显示有用的命令
Write-Host "常用命令:" -ForegroundColor Yellow
Write-Host "  查看容器日志: docker logs -f payment-gateway" -ForegroundColor Gray
Write-Host "  重启容器: docker-compose restart" -ForegroundColor Gray
Write-Host "  查看容器状态: docker ps" -ForegroundColor Gray
Write-Host ""
