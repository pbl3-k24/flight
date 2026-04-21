# Flight Booking System - Sample cURL Requests for Testing
# Chạy file này trong PowerShell để test tất cả endpoints

$BaseUrl = "http://localhost:5042"
$AdminToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwiZW1haWwiOiJhZG1pbkBmbGlnaHRib29raW5nLnZuIiwidW5pcXVlX25hbWUiOiJRdeG6o24gdHLhu4sgdmnDqm4iLCJyb2xlIjoiQWRtaW4iLCJuYmYiOjE3NzY3Nzg5MDIsImV4cCI6MTc3Njg2NTMwMSwiaWF0IjoxNzc2Nzc4OTAyLCJpc3MiOiJmbGlnaHQtYm9va2luZy1hcGkiLCJhdWQiOiJmbGlnaHQtYm9va2luZy1hcHAifQ.LQweCjrcMTouo0h4rfSKu2mqgOl_UeNxvERu0w14QFI"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  FLIGHT BOOKING SYSTEM - TEST CASES" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# ========================================
# 1. LOGIN
# ========================================
Write-Host "[1] LOGIN - Get Admin Token" -ForegroundColor Yellow
Write-Host "POST /api/v1/Users/login" -ForegroundColor Gray
$loginBody = @{
    email = "admin@flightbooking.vn"
    password = "Admin@123456"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/Users/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -UseBasicParsing
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 2
    Write-Host "✅ Login successful" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to continue..."
Write-Host ""

# ========================================
# 2. SEARCH FLIGHTS - HCM to Hanoi
# ========================================
Write-Host "[2] SEARCH FLIGHTS - HCM to Hanoi" -ForegroundColor Yellow
Write-Host "GET /api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-22" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-22" `
        -Method GET `
        -Headers @{"Accept" = "application/json"} `
        -UseBasicParsing
    $flights = $response.Content | ConvertFrom-Json
    Write-Host "✅ Found $($flights.Count) flights" -ForegroundColor Green
    $flights | Select-Object -First 3 | ConvertTo-Json -Depth 2
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to continue..."
Write-Host ""

# ========================================
# 3. GET ALL FLIGHTS (Admin) - Page 1
# ========================================
Write-Host "[3] GET FLIGHTS (Admin) - Page 1" -ForegroundColor Yellow
Write-Host "GET /api/v1/admin/FlightsAdmin?page=1&pageSize=10" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/admin/FlightsAdmin?page=1&pageSize=10" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $AdminToken"
            "Accept" = "application/json"
        } `
        -UseBasicParsing
    $data = $response.Content | ConvertFrom-Json
    Write-Host "✅ Total flights: $($data.totalCount)" -ForegroundColor Green
    $data.data | Select-Object -First 3 | ConvertTo-Json -Depth 2
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to continue..."
Write-Host ""

# ========================================
# 4. GET ALL PROMOTIONS (Admin)
# ========================================
Write-Host "[4] GET PROMOTIONS (Admin)" -ForegroundColor Yellow
Write-Host "GET /api/v1/admin/PromotionsAdmin?page=1&pageSize=10" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/admin/PromotionsAdmin?page=1&pageSize=10" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $AdminToken"
            "Accept" = "application/json"
        } `
        -UseBasicParsing
    $data = $response.Content | ConvertFrom-Json
    Write-Host "✅ Total promotions: $($data.totalCount)" -ForegroundColor Green
    $data.data | Select-Object -First 3 | ConvertTo-Json -Depth 2
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to continue..."
Write-Host ""

# ========================================
# 5. VALIDATE PROMOTION CODE - SUMMER20
# ========================================
Write-Host "[5] VALIDATE PROMOTION CODE" -ForegroundColor Yellow
Write-Host "POST /api/v1/promotions/validate" -ForegroundColor Gray

$validateBody = @{
    code = "SUMMER20"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/promotions/validate" `
        -Method POST `
        -ContentType "application/json" `
        -Body $validateBody `
        -UseBasicParsing
    $promo = $response.Content | ConvertFrom-Json
    if ($promo.isValid) {
        Write-Host "✅ Valid promotion: $($promo.code)" -ForegroundColor Green
        Write-Host "   Discount: $($promo.discountValue)$( if($promo.discountType -eq 0) {'%'} else {'VND'} )" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Invalid promotion" -ForegroundColor Red
    }
    $promo | ConvertTo-Json -Depth 2
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to continue..."
Write-Host ""

# ========================================
# 6. TEST PAGINATION - Page 2
# ========================================
Write-Host "[6] GET FLIGHTS - Page 2" -ForegroundColor Yellow
Write-Host "GET /api/v1/admin/FlightsAdmin?page=2&pageSize=10" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/admin/FlightsAdmin?page=2&pageSize=10" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $AdminToken"
            "Accept" = "application/json"
        } `
        -UseBasicParsing
    $data = $response.Content | ConvertFrom-Json
    Write-Host "✅ Page 2 - Items: $($data.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to continue..."
Write-Host ""

# ========================================
# 7. SEARCH - HCM to Da Nang
# ========================================
Write-Host "[7] SEARCH FLIGHTS - HCM to Da Nang" -ForegroundColor Yellow
Write-Host "GET /api/v1/flights/search?departureId=1&arrivalId=3&date=2025-01-22" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/flights/search?departureId=1&arrivalId=3&date=2025-01-22" `
        -Method GET `
        -Headers @{"Accept" = "application/json"} `
        -UseBasicParsing
    $flights = $response.Content | ConvertFrom-Json
    Write-Host "✅ Found $($flights.Count) flights on this route" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to continue..."
Write-Host ""

# ========================================
# 8. INVALID PROMO CODE
# ========================================
Write-Host "[8] VALIDATE INVALID CODE" -ForegroundColor Yellow
Write-Host "POST /api/v1/promotions/validate" -ForegroundColor Gray

$invalidBody = @{
    code = "INVALIDCODE999"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/v1/promotions/validate" `
        -Method POST `
        -ContentType "application/json" `
        -Body $invalidBody `
        -UseBasicParsing
    $promo = $response.Content | ConvertFrom-Json
    if (-not $promo.isValid) {
        Write-Host "✅ Correctly rejected invalid code" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to continue..."
Write-Host ""

# ========================================
# 9. STATISTICS
# ========================================
Write-Host "[9] DATA STATISTICS" -ForegroundColor Yellow

try {
    $flightsResponse = Invoke-WebRequest -Uri "$BaseUrl/api/v1/admin/FlightsAdmin?page=1&pageSize=1" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $AdminToken"
            "Accept" = "application/json"
        } `
        -UseBasicParsing
    $flightsData = $flightsResponse.Content | ConvertFrom-Json

    $promosResponse = Invoke-WebRequest -Uri "$BaseUrl/api/v1/admin/PromotionsAdmin?page=1&pageSize=1" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $AdminToken"
            "Accept" = "application/json"
        } `
        -UseBasicParsing
    $promosData = $promosResponse.Content | ConvertFrom-Json

    Write-Host ""
    Write-Host "📊 DATA SUMMARY:" -ForegroundColor Cyan
    Write-Host "   Total Flights: $($flightsData.totalCount)" -ForegroundColor Yellow
    Write-Host "   Total Promotions: $($promosData.totalCount)" -ForegroundColor Yellow
    Write-Host ""
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "✅ All tests completed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
