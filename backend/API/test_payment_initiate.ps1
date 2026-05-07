# Test Payment Initiate API

Write-Host "Testing Payment Initiate API..." -ForegroundColor Yellow
Write-Host ""

# Bước 1: Login để lấy token
Write-Host "Step 1: Login to get token..." -ForegroundColor Cyan
$loginBody = @{
    email = "admin@flightbooking.vn"
    password = "Admin@123456"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5042/api/v1/Users/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "  Token obtained: $($token.Substring(0, 20))..." -ForegroundColor Green
} catch {
    Write-Host "  Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Bước 2: Tạo booking (giả sử có flight ID 1)
Write-Host "Step 2: Create booking..." -ForegroundColor Cyan
$bookingBody = @{
    outboundFlightId = 1
    passengerCount = 1
    seatClassId = 1
    passengers = @(
        @{
            firstName = "Nguyen"
            lastName = "Van A"
            email = "test@gmail.com"
            phone = "0901234567"
            dateOfBirth = "1990-01-01"
            nationality = "Vietnam"
            passportNumber = "A1234567"
        }
    )
    contactEmail = "test@gmail.com"
} | ConvertTo-Json -Depth 10

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    $bookingResponse = Invoke-RestMethod -Uri "http://localhost:5042/api/v1/Bookings" -Method POST -Body $bookingBody -Headers $headers
    $bookingId = $bookingResponse.bookingId
    Write-Host "  Booking created: ID = $bookingId" -ForegroundColor Green
} catch {
    Write-Host "  Booking creation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Bước 3: Initiate payment
Write-Host "Step 3: Initiate payment..." -ForegroundColor Cyan
$paymentBody = @{
    bookingId = $bookingId
    paymentMethod = "VNPAY"
} | ConvertTo-Json

try {
    $paymentResponse = Invoke-RestMethod -Uri "http://localhost:5042/api/v1/Payments" -Method POST -Body $paymentBody -Headers $headers
    
    Write-Host "  Payment initiated successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Payment Response:" -ForegroundColor Cyan
    Write-Host "  Payment ID: $($paymentResponse.paymentId)" -ForegroundColor White
    Write-Host "  Booking ID: $($paymentResponse.bookingId)" -ForegroundColor White
    Write-Host "  Status: $($paymentResponse.status)" -ForegroundColor White
    Write-Host "  Amount: $($paymentResponse.amount)" -ForegroundColor White
    Write-Host "  Provider: $($paymentResponse.provider)" -ForegroundColor White
    Write-Host "  Transaction Ref: $($paymentResponse.transactionRef)" -ForegroundColor White
    Write-Host ""
    
    if ($paymentResponse.paymentLink) {
        Write-Host "  Payment Link: $($paymentResponse.paymentLink.Substring(0, 100))..." -ForegroundColor Green
        Write-Host ""
        Write-Host "  Full Payment Link:" -ForegroundColor Yellow
        Write-Host "  $($paymentResponse.paymentLink)" -ForegroundColor Gray
    } else {
        Write-Host "  WARNING: PaymentLink is NULL or empty!" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Full Response JSON:" -ForegroundColor Cyan
    $paymentResponse | ConvertTo-Json -Depth 10
    
} catch {
    Write-Host "  Payment initiation failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Test completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
