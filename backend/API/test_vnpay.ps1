# Test VNPay Payment URL Generation
Write-Host "Testing VNPay Payment URL..." -ForegroundColor Cyan

# Simulate parameters
$tmnCode = "DEMOV210"
$hashSecret = "RAOEXHYVSDDIIENYWSLDIIZTANXUXZFJ"
$baseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
$amount = 10000000  # 100,000 VND x 100
$txnRef = "123456789"
$orderInfo = "Thanh toan dat ve 1"
$returnUrl = "http://localhost:5042/api/v1/payments/vnpay-return"
$createDate = Get-Date -Format "yyyyMMddHHmmss"

Write-Host "TmnCode: $tmnCode" -ForegroundColor Yellow
Write-Host "Amount: $amount" -ForegroundColor Yellow
Write-Host "TxnRef: $txnRef" -ForegroundColor Yellow
Write-Host "OrderInfo: $orderInfo" -ForegroundColor Yellow
Write-Host "CreateDate: $createDate" -ForegroundColor Yellow

# Build parameters (sorted)
$params = @{
    "vnp_Amount" = $amount
    "vnp_Command" = "pay"
    "vnp_CreateDate" = $createDate
    "vnp_CurrCode" = "VND"
    "vnp_IpAddr" = "113.161.84.26"
    "vnp_Locale" = "vn"
    "vnp_OrderInfo" = $orderInfo
    "vnp_OrderType" = "other"
    "vnp_ReturnUrl" = $returnUrl
    "vnp_TmnCode" = $tmnCode
    "vnp_TxnRef" = $txnRef
    "vnp_Version" = "2.1.0"
}

# Sort and build hash data
$sortedParams = $params.GetEnumerator() | Sort-Object Name
$hashData = ($sortedParams | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join "&"

Write-Host "`nHash Data:" -ForegroundColor Green
Write-Host $hashData

# Build URL
$queryString = ($sortedParams | ForEach-Object { 
    "$($_.Key)=$([System.Uri]::EscapeDataString($_.Value))" 
}) -join "&"

Write-Host "`nQuery String:" -ForegroundColor Green
Write-Host $queryString

Write-Host "`nFull URL (without hash):" -ForegroundColor Magenta
Write-Host "$baseUrl?$queryString"
