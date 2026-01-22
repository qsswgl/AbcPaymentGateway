# 农行一码多扫接口测试脚本
# Usage: .\Test-AbcScanPay.ps1

param(
    [string]$BaseUrl = "https://payment.qsgl.net",
    [decimal]$Amount = 0.01,
    [string]$GoodsName = "测试商品",
    [string]$MerchantId = "103881636900016"
)

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$orderNo = "SCANPAY_${timestamp}"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " 农行一码多扫接口测试" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "订单号: $orderNo" -ForegroundColor Yellow
Write-Host "金额: $Amount" -ForegroundColor Yellow
Write-Host "商户号: $MerchantId" -ForegroundColor Yellow
Write-Host ""

$requestBody = @{
    orderNo = $orderNo
    amount = $Amount
    merchantId = $MerchantId
    goodsName = $GoodsName
    notifyUrl = "$BaseUrl/api/payment/notify"
    payTypeID = "ImmediatePay"
    paymentType = "A"
    commodityType = "0201"
    paymentLinkType = "1"
} | ConvertTo-Json -Compress

Write-Host "请求体:" -ForegroundColor Green
Write-Host $requestBody -ForegroundColor Gray
Write-Host ""

try {
    Write-Host "发送请求..." -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/payment/abc/scanpay" `
        -Method POST `
        -Body $requestBody `
        -ContentType "application/json; charset=utf-8" `
        -ErrorAction Stop
    
    Write-Host "✓ 请求成功" -ForegroundColor Green
    Write-Host ""
    Write-Host "响应数据:" -ForegroundColor Green
    $response | Format-List
    
    if ($response.isSuccess) {
        Write-Host "✓ 支付下单成功" -ForegroundColor Green
        Write-Host "二维码URL: $($response.scanPayQRURL)" -ForegroundColor Cyan
        
        # 生成二维码（如果安装了qrcode模块）
        if (Get-Command New-QRCode -ErrorAction SilentlyContinue) {
            Write-Host ""
            Write-Host "生成二维码..." -ForegroundColor Yellow
            New-QRCode -Data $response.scanPayQRURL -OutFile "qrcode_${orderNo}.png"
            Write-Host "✓ 二维码已保存: qrcode_${orderNo}.png" -ForegroundColor Green
        }
    } else {
        Write-Host "✗ 支付下单失败" -ForegroundColor Red
        Write-Host "错误码: $($response.errorCode)" -ForegroundColor Red
        Write-Host "错误信息: $($response.message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "状态: $($response.status)" -ForegroundColor Yellow
        
        # 错误分析
        switch ($response.errorCode) {
            "EUNKWN" {
                Write-Host ""
                Write-Host "⚠ EUNKWN错误分析:" -ForegroundColor Yellow
                Write-Host "  - 可能原因1: 一码多扫功能未开通" -ForegroundColor Gray
                Write-Host "  - 可能原因2: 商户权限配置问题" -ForegroundColor Gray
                Write-Host "  - 可能原因3: 测试环境配置不正确" -ForegroundColor Gray
                Write-Host "  - 建议: 联系农行技术支持确认功能开通状态" -ForegroundColor Cyan
            }
            "APE009" {
                Write-Host ""
                Write-Host "⚠ APE009错误分析:" -ForegroundColor Yellow
                Write-Host "  - 请求报文格式错误，请检查必填字段" -ForegroundColor Gray
                Write-Host "  - 建议: 查看服务器日志，对比发送的报文" -ForegroundColor Cyan
            }
            "APE400" {
                Write-Host ""
                Write-Host "⚠ APE400错误分析:" -ForegroundColor Yellow
                Write-Host "  - 签名验证失败" -ForegroundColor Gray
                Write-Host "  - 建议: 检查证书配置和签名算法" -ForegroundColor Cyan
            }
            "APE002" {
                Write-Host ""
                Write-Host "⚠ APE002错误分析:" -ForegroundColor Yellow
                Write-Host "  - 商户信息不存在" -ForegroundColor Gray
                Write-Host "  - 建议: 检查商户号配置" -ForegroundColor Cyan
            }
            "APE003" {
                Write-Host ""
                Write-Host "⚠ APE003错误分析:" -ForegroundColor Yellow
                Write-Host "  - 商户未开通此功能" -ForegroundColor Gray
                Write-Host "  - 建议: 联系农行开通一码多扫功能" -ForegroundColor Cyan
            }
        }
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✗ 请求失败" -ForegroundColor Red
    Write-Host "HTTP状态码: $statusCode" -ForegroundColor Red
    Write-Host "错误信息: $($_.Exception.Message)" -ForegroundColor Red
    
    # 尝试获取错误响应体
    try {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $errorBody = $reader.ReadToEnd()
        
        Write-Host ""
        Write-Host "错误响应:" -ForegroundColor Yellow
        $errorBody | ConvertFrom-Json | Format-List
    } catch {
        # 忽略解析错误
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "测试完成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "=====================================" -ForegroundColor Cyan
