# ABC ScanPay Test Script
param(
    [string]$BaseUrl = "https://payment.qsgl.net",
    [decimal]$Amount = 0.01
)

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$orderNo = "SCANPAY_$timestamp"

Write-Host "Testing ABC ScanPay API..." -ForegroundColor Cyan
Write-Host "Order No: $orderNo" -ForegroundColor Yellow
Write-Host "Amount: $Amount" -ForegroundColor Yellow

$body = @{
    orderNo = $orderNo
    amount = $Amount
    merchantId = "103881636900016"
    goodsName = "Test Product"
    notifyUrl = "$BaseUrl/api/payment/notify"
    payTypeID = "ImmediatePay"
    paymentType = "A"
    commodityType = "0201"
} | ConvertTo-Json -Compress

try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/payment/abc/scanpay" -Method POST -Body $body -ContentType "application/json; charset=utf-8"
    Write-Host "Success!" -ForegroundColor Green
    $response | Format-List
    
    if (!$response.isSuccess) {
        Write-Host "Error Code: $($response.errorCode)" -ForegroundColor Red
        Write-Host "Message: $($response.message)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Request failed: $($_.Exception.Message)" -ForegroundColor Red
}
