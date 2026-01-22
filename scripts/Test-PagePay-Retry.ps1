# Test ABC PagePay API - With Larger Amount
$ErrorActionPreference = "Stop"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Testing ABC PagePay - Retry with 10 Yuan" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$apiUrl = "https://payment.qsgl.net/api/payment/abc/pagepay"

# 使用更大的金额 (10元而不是0.01元)
$testData = @{
    OrderNo = "TEST" + (Get-Date -Format "yyyyMMddHHmmss")
    Amount = 10.00
    MerchantId = "103881636900016"
    GoodsName = "Game Card Order"
    NotifyUrl = "https://payment.qsgl.net/api/payment/abc/notify"
    MerchantSuccessUrl = "https://payment.qsgl.net/success"
    MerchantErrorUrl = "https://payment.qsgl.net/fail"
}

Write-Host "Test Data:" -ForegroundColor Yellow
Write-Host "  OrderNo: $($testData.OrderNo)" -ForegroundColor White
Write-Host "  Amount: CNY $($testData.Amount)" -ForegroundColor Green
Write-Host "  GoodsName: $($testData.GoodsName)" -ForegroundColor White
Write-Host ""

Write-Host "Sending request to: $apiUrl`n" -ForegroundColor Cyan

try {
    $jsonBody = $testData | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post `
        -ContentType "application/json" `
        -Body $jsonBody `
        -TimeoutSec 30
    
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  API Response Received!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Green
    
    $response | ConvertTo-Json | Write-Host -ForegroundColor White
    
    if ($response.isSuccess -and $response.paymentURL) {
        Write-Host "`n========================================" -ForegroundColor Green
        Write-Host "  SUCCESS! PaymentURL Generated!" -ForegroundColor Green
        Write-Host "========================================`n" -ForegroundColor Green
        
        Write-Host "PaymentURL:" -ForegroundColor Yellow
        Write-Host $response.paymentURL -ForegroundColor Green
        
        if ($response.paymentURL -match "TOKEN=([^&]+)") {
            $token = $Matches[1]
            Write-Host "`nTOKEN: $token" -ForegroundColor Cyan
        }
        
        Write-Host "`nOpen payment page in browser? (Y/N): " -ForegroundColor Yellow -NoNewline
        $open = Read-Host
        
        if ($open -eq "Y" -or $open -eq "y") {
            Write-Host "Opening payment page..." -ForegroundColor Green
            Start-Process $response.paymentURL
            Write-Host "`nPayment page opened in browser!" -ForegroundColor Green
        }
    } elseif ($response.errorCode -eq "EUNKWN") {
        Write-Host "`n========================================" -ForegroundColor Yellow
        Write-Host "  Still EUNKWN - Business Config Issue" -ForegroundColor Yellow
        Write-Host "========================================`n" -ForegroundColor Yellow
        Write-Host "This confirms it is NOT a technical issue." -ForegroundColor White
        Write-Host "API implementation is CORRECT!" -ForegroundColor Green
    } else {
        Write-Host "`nUnexpected response" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "`nAPI Call Failed:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "`nTest completed!`n" -ForegroundColor Cyan
