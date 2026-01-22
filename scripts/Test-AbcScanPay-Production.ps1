# 农行一码多扫线上扫码下单测试脚本（生产环境）
# 测试日期: 2026-01-19 08:45
# 测试金额: 10元

param(
    [string]$ServerUrl = "https://tx.qsgl.net",
    [decimal]$Amount = 10.00,
    [string]$OrderTime = "20260119084500"
)

# 设置控制台编码
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# 生成订单号：日期时间戳
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$orderNo = "ABC_SCAN_$timestamp"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  农行一码多扫线上扫码下单测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "测试环境：生产环境" -ForegroundColor Yellow
Write-Host "服务器地址：$ServerUrl" -ForegroundColor Yellow
Write-Host "订单号：$orderNo" -ForegroundColor Yellow
Write-Host "订单时间：2026-01-19 08:45:00" -ForegroundColor Yellow
Write-Host "支付金额：¥$Amount 元" -ForegroundColor Green
Write-Host ""

# 构建请求参数
$requestBody = @{
    orderNo = $orderNo
    amount = $Amount
    merchantId = "103881636900016"
    goodsName = "测试商品-结算单"
    orderDesc = "结算单金额测试-2026年1月19日08:45"
    notifyUrl = "$ServerUrl/api/payment/notify"
    payTypeID = "ImmediatePay"
    paymentType = "A"
    paymentLinkType = "1"
    commodityType = "0201"
    orderTime = "20260119084500"
}


$requestBodyJson = $requestBody | ConvertTo-Json -Compress

Write-Host "请求参数：" -ForegroundColor Cyan
Write-Host $requestBodyJson -ForegroundColor Gray
Write-Host ""

# 发送请求
Write-Host "正在发送请求到农行支付网关..." -ForegroundColor Cyan

try {
    $apiUrl = "$ServerUrl/api/payment/abc/scanpay"
    
    $response = Invoke-RestMethod -Uri $apiUrl `
        -Method POST `
        -Body $requestBodyJson `
        -ContentType "application/json; charset=utf-8" `
        -TimeoutSec 30
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  响应结果" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    # 显示完整响应
    $response | Format-List
    
    # 判断结果
    if ($response.isSuccess) {
        Write-Host ""
        Write-Host "✓ 下单成功！" -ForegroundColor Green
        Write-Host ""
        Write-Host "订单号：$($response.orderNo)" -ForegroundColor Yellow
        Write-Host "交易ID：$($response.transactionId)" -ForegroundColor Yellow
        Write-Host "支付金额：¥$($response.amount)" -ForegroundColor Green
        Write-Host "状态：$($response.status)" -ForegroundColor Green
        Write-Host "过期时间：$($response.expireTime)" -ForegroundColor Yellow
        Write-Host ""
        
        if ($response.scanPayQRURL) {
            Write-Host "二维码链接：" -ForegroundColor Cyan
            Write-Host $response.scanPayQRURL -ForegroundColor White
            Write-Host ""
            
            # 生成二维码
            Write-Host "正在生成二维码..." -ForegroundColor Cyan
            
            # 使用在线API生成二维码并保存
            $qrUrl = $response.scanPayQRURL
            $encodedUrl = [System.Web.HttpUtility]::UrlEncode($qrUrl)
            $qrCodeApiUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=$encodedUrl"
            
            # 保存二维码图片
            $outputDir = "K:\payment\AbcPaymentGateway\Scripts\QRCodes"
            if (-not (Test-Path $outputDir)) {
                New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
            }
            
            $qrCodeFile = Join-Path $outputDir "$orderNo.png"
            
            try {
                Invoke-WebRequest -Uri $qrCodeApiUrl -OutFile $qrCodeFile
                Write-Host "✓ 二维码已保存到：$qrCodeFile" -ForegroundColor Green
                
                # 在Windows上打开图片
                Start-Process $qrCodeFile
                
                Write-Host ""
                Write-Host "二维码已在默认图片查看器中打开" -ForegroundColor Cyan
            }
            catch {
                Write-Host "✗ 二维码生成失败：$($_.Exception.Message)" -ForegroundColor Red
                Write-Host ""
                Write-Host "请手动访问以下链接生成二维码：" -ForegroundColor Yellow
                Write-Host "https://cli.im/url/?text=$encodedUrl" -ForegroundColor White
            }
            
            Write-Host ""
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host "  扫码支付说明" -ForegroundColor Cyan
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host "1. 使用微信、支付宝、云闪付等APP扫描二维码" -ForegroundColor Yellow
            Write-Host "2. 在APP中完成支付" -ForegroundColor Yellow
            Write-Host "3. 支付成功后会收到回调通知" -ForegroundColor Yellow
            Write-Host ""
        }
        else {
            Write-Host "⚠ 警告：未返回二维码链接" -ForegroundColor Yellow
        }
    }
    elseif ($response.status -eq "UNKNOWN") {
        Write-Host ""
        Write-Host "⚠ 交易结果未知 (EUNKWN)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "错误代码：$($response.errorCode)" -ForegroundColor Yellow
        Write-Host "错误信息：$($response.message)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "可能原因：" -ForegroundColor Cyan
        Write-Host "1. 商户功能未完全开通" -ForegroundColor White
        Write-Host "2. 农行系统繁忙" -ForegroundColor White
        Write-Host "3. 网络超时" -ForegroundColor White
        Write-Host ""
        Write-Host "建议操作：" -ForegroundColor Cyan
        Write-Host "1. 等待5-10秒后查询订单状态" -ForegroundColor White
        Write-Host "2. 联系农行确认商户权限" -ForegroundColor White
        Write-Host "3. 检查服务器日志" -ForegroundColor White
    }
    else {
        Write-Host ""
        Write-Host "✗ 下单失败" -ForegroundColor Red
        Write-Host ""
        Write-Host "错误代码：$($response.errorCode)" -ForegroundColor Red
        Write-Host "错误信息：$($response.message)" -ForegroundColor Yellow
        Write-Host "状态：$($response.status)" -ForegroundColor Yellow
        Write-Host ""
        
        Write-Host "常见错误排查：" -ForegroundColor Cyan
        Write-Host "1. EUNKWN - 交易结果未知，需查询订单" -ForegroundColor White
        Write-Host "2. 9001 - 商户未开通功能或权限不足" -ForegroundColor White
        Write-Host "3. 9002 - 签名验证失败，检查证书配置" -ForegroundColor White
        Write-Host "4. 9003 - 参数格式错误" -ForegroundColor White
        Write-Host ""
    }
    
    # 保存测试结果到文件
    $resultFile = "K:\payment\AbcPaymentGateway\Scripts\test_result_$timestamp.json"
    $response | ConvertTo-Json -Depth 10 | Out-File -FilePath $resultFile -Encoding UTF8
    Write-Host "完整响应已保存到：$resultFile" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "✗ 请求失败" -ForegroundColor Red
    Write-Host ""
    Write-Host "错误信息：$($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "HTTP状态码：$($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "排查建议：" -ForegroundColor Cyan
    Write-Host "1. 检查服务器是否正常运行" -ForegroundColor White
    Write-Host "2. 验证URL是否正确：$ServerUrl" -ForegroundColor White
    Write-Host "3. 检查网络连接" -ForegroundColor White
    Write-Host "4. 查看服务器日志：" -ForegroundColor White
    Write-Host "   ssh -i 'K:\Key\tx.qsgl.net_id_ed25519' root@tx.qsgl.net 'docker logs payment-gateway --tail 50'" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "测试完成。" -ForegroundColor Cyan
Write-Host ""
