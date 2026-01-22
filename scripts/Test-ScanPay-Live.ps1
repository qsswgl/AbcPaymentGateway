# ABC ScanPay Live Test Script
param(
    [string]$ServerUrl = "https://tx.qsgl.net",
    [decimal]$Amount = 10.00
)

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$orderNo = "ABC_SCAN_$timestamp"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ABC Scan Pay Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Server: $ServerUrl" -ForegroundColor Yellow
Write-Host "Order No: $orderNo" -ForegroundColor Yellow
Write-Host "Amount: CNY $Amount" -ForegroundColor Green
Write-Host "Time: 2026-01-19 08:45:00" -ForegroundColor Yellow
Write-Host ""

$requestBody = @{
    orderNo = $orderNo
    amount = $Amount
    merchantId = "103881636900016"
    goodsName = "Test Product"
    orderDesc = "Settlement Test 2026-01-19 08:45"
    notifyUrl = "$ServerUrl/api/payment/notify"
    payTypeID = "ImmediatePay"
    paymentType = "A"
    paymentLinkType = "1"
    commodityType = "0201"
    orderTime = "20260119084500"
}

$jsonBody = $requestBody | ConvertTo-Json -Compress

Write-Host "Request Body:" -ForegroundColor Cyan
Write-Host $jsonBody -ForegroundColor Gray
Write-Host ""

Write-Host "Sending request..." -ForegroundColor Cyan

try {
    $apiUrl = "$ServerUrl/api/payment/abc/scanpay"
    
    $response = Invoke-RestMethod -Uri $apiUrl -Method POST -Body $jsonBody -ContentType "application/json; charset=utf-8" -TimeoutSec 30
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Response" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    $response | Format-List
    
    if ($response.isSuccess) {
        Write-Host ""
        Write-Host "SUCCESS!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Order No: $($response.orderNo)" -ForegroundColor Yellow
        Write-Host "Transaction ID: $($response.transactionId)" -ForegroundColor Yellow
        Write-Host "Amount: CNY $($response.amount)" -ForegroundColor Green
        Write-Host ""
        
        if ($response.scanPayQRURL) {
            Write-Host "QR Code URL:" -ForegroundColor Cyan
            Write-Host $response.scanPayQRURL -ForegroundColor White
            Write-Host ""
            
            Write-Host "Generating QR Code..." -ForegroundColor Cyan
            
            $qrUrl = $response.scanPayQRURL
            $encodedUrl = [uri]::EscapeDataString($qrUrl)
            $qrCodeApiUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300`&data=$encodedUrl"
            
            $outputDir = "K:\payment\AbcPaymentGateway\Scripts\QRCodes"
            if (-not (Test-Path $outputDir)) {
                New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
            }
            
            $qrCodeFile = Join-Path $outputDir "$orderNo.png"
            
            try {
                Invoke-WebRequest -Uri $qrCodeApiUrl -OutFile $qrCodeFile
                Write-Host "QR Code saved: $qrCodeFile" -ForegroundColor Green
                Start-Process $qrCodeFile
                Write-Host "QR Code opened in default viewer" -ForegroundColor Cyan
            }
            catch {
                Write-Host "QR Code generation failed: $($_.Exception.Message)" -ForegroundColor Red
            }
            
            Write-Host ""
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host "How to pay:" -ForegroundColor Cyan
            Write-Host "1. Scan QR code with WeChat/Alipay/UnionPay" -ForegroundColor Yellow
            Write-Host "2. Complete payment in the app" -ForegroundColor Yellow
            Write-Host "3. Wait for callback notification" -ForegroundColor Yellow
            Write-Host ""
        }
    }
    elseif ($response.status -eq "UNKNOWN") {
        Write-Host ""
        Write-Host "Transaction result UNKNOWN (EUNKWN)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Error Code: $($response.errorCode)" -ForegroundColor Yellow
        Write-Host "Message: $($response.message)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Suggestion: Query order status after 5-10 seconds" -ForegroundColor Cyan
    }
    else {
        Write-Host ""
        Write-Host "FAILED" -ForegroundColor Red
        Write-Host ""
        Write-Host "Error Code: $($response.errorCode)" -ForegroundColor Red
        Write-Host "Message: $($response.message)" -ForegroundColor Yellow
    }
    
    $resultFile = "K:\payment\AbcPaymentGateway\Scripts\test_result_$timestamp.json"
    $response | ConvertTo-Json -Depth 10 | Out-File -FilePath $resultFile -Encoding UTF8
    Write-Host "Response saved to: $resultFile" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "REQUEST FAILED" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "HTTP Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

Write-Host "Test complete." -ForegroundColor Cyan
Write-Host ""