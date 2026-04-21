# JWT Authentication Test Script
# Usage: powershell .\jwt-test.ps1

$baseUrl = "http://localhost:5042"
$adminEmail = "admin@flightbooking.vn"
$adminPassword = "Admin@123456"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "JWT Authentication Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: No Auth endpoint
Write-Host "[1/6] Testing no-auth endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/debug/no-auth" -Method Get
    Write-Host "✅ No-Auth: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json | Write-Host -ForegroundColor Gray
} catch {
    Write-Host "❌ No-Auth failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Login
Write-Host "[2/6] Testing login endpoint..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $adminEmail
        password = $adminPassword
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/users/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody

    $loginResponse = $response.Content | ConvertFrom-Json
    $token = $loginResponse.token

    Write-Host "✅ Login successful: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Token (first 50 chars): $($token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host "Expires: $($loginResponse.expiresAt)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 3: With Auth (requires token)
Write-Host "[3/6] Testing with-auth endpoint (requires token)..." -ForegroundColor Yellow
try {
    $headers = @{
        Authorization = "Bearer $token"
    }

    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/debug/with-auth" `
        -Method Get `
        -Headers $headers

    Write-Host "✅ With-Auth: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json | Write-Host -ForegroundColor Gray
} catch {
    Write-Host "❌ With-Auth failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Response: $($_.Exception.Response.Content | ConvertFrom-Json | ConvertTo-Json)" -ForegroundColor Red
    }
}
Write-Host ""

# Test 4: Admin Only (requires Admin role)
Write-Host "[4/6] Testing admin-only endpoint (requires Admin role)..." -ForegroundColor Yellow
try {
    $headers = @{
        Authorization = "Bearer $token"
    }

    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/debug/admin-only" `
        -Method Get `
        -Headers $headers

    Write-Host "✅ Admin-Only: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json | Write-Host -ForegroundColor Gray
} catch {
    Write-Host "❌ Admin-Only failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Response: $($_.Exception.Response.Content | ConvertFrom-Json | ConvertTo-Json)" -ForegroundColor Red
    }
}
Write-Host ""

# Test 5: Show Claims
Write-Host "[5/6] Testing claims endpoint (show all token claims)..." -ForegroundColor Yellow
try {
    $headers = @{
        Authorization = "Bearer $token"
    }

    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/debug/claims" `
        -Method Get `
        -Headers $headers

    Write-Host "✅ Claims: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json | Write-Host -ForegroundColor Gray
} catch {
    Write-Host "❌ Claims failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 6: Real Admin Endpoint
Write-Host "[6/6] Testing real admin endpoint..." -ForegroundColor Yellow
try {
    $headers = @{
        Authorization = "Bearer $token"
    }

    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/admin/FlightsAdmin?page=1&pageSize=10" `
        -Method Get `
        -Headers $headers

    Write-Host "✅ FlightsAdmin: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response data: $($response.Content.Length) bytes" -ForegroundColor Gray
} catch {
    Write-Host "❌ FlightsAdmin failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test completed!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
