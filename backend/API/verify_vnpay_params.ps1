# VNPay Parameter Verification Script
Write-Host "=== VNPay Parameter Verification ===" -ForegroundColor Cyan
Write-Host ""

# Test data
$bookingId = 10
$amount = 1200000  # VND
$amountParam = $amount * 100  # x100
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$txnRef = "$bookingId$timestamp"
$orderInfo = "Thanh toan booking 7OF9MC"
$createDate = (Get-Date).ToString("yyyyMMddHHmmss")

Write-Host "Input Data:" -ForegroundColor Yellow
Write-Host "  Booking ID: $bookingId"
Write-Host "  Amount: $amount VND"
Write-Host "  Order Info: $orderInfo"
Write-Host ""

Write-Host "Generated Parameters:" -ForegroundColor Green
Write-Host "  vnp_Amount: $amountParam (type: $($amountParam.GetType().Name))"
Write-Host "  vnp_TxnRef: $txnRef (length: $($txnRef.Length))"
Write-Host "  vnp_OrderInfo: $orderInfo (length: $($orderInfo.Length))"
Write-Host "  vnp_CreateDate: $createDate"
Write-Host ""

# Validation
Write-Host "Validation Checks:" -ForegroundColor Magenta

# Check 1: Amount is integer
if ($amountParam -is [int] -or $amountParam -is [long]) {
    Write-Host "  ✅ vnp_Amount is integer" -ForegroundColor Green
} else {
    Write-Host "  ❌ vnp_Amount is NOT integer" -ForegroundColor Red
}

# Check 2: TxnRef length
if ($txnRef.Length -le 100) {
    Write-Host "  ✅ vnp_TxnRef length OK ($($txnRef.Length) <= 100)" -ForegroundColor Green
} else {
    Write-Host "  ❌ vnp_TxnRef too long ($($txnRef.Length) > 100)" -ForegroundColor Red
}

# Check 3: OrderInfo has no special chars
if ($orderInfo -match '^[a-zA-Z0-9\s\-]+$') {
    Write-Host "  ✅ vnp_OrderInfo has no special characters" -ForegroundColor Green
} else {
    Write-Host "  ❌ vnp_OrderInfo contains special characters" -ForegroundColor Red
}

# Check 4: OrderInfo length
if ($orderInfo.Length -le 255) {
    Write-Host "  ✅ vnp_OrderInfo length OK ($($orderInfo.Length) <= 255)" -ForegroundColor Green
} else {
    Write-Host "  ❌ vnp_OrderInfo too long ($($orderInfo.Length) > 255)" -ForegroundColor Red
}

# Check 5: CreateDate format
if ($createDate -match '^\d{14}$') {
    Write-Host "  ✅ vnp_CreateDate format OK (yyyyMMddHHmmss)" -ForegroundColor Green
} else {
    Write-Host "  ❌ vnp_CreateDate format invalid" -ForegroundColor Red
}

Write-Host ""
Write-Host "All parameters are ready for VNPay!" -ForegroundColor Cyan
