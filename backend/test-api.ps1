#!/usr/bin/env powershell

# Flight Booking API - Quick Test Script
# Sử dụng script này để test login và lấy JWT token

param(
    [string]$Email = "admin@flightbooking.vn",
    [string]$Password = "Admin@123456",
    [string]$ApiUrl = "http://localhost:5000"
)

Write-Host "🚀 Flight Booking API - Quick Test" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Check if API is running
Write-Host "`n🔍 Checking API health..." -ForegroundColor Yellow
$healthCheck = $null
try {
    $healthCheck = Invoke-WebRequest -Uri "$ApiUrl/health" -ErrorAction SilentlyContinue
    if ($healthCheck.StatusCode -eq 200) {
        Write-Host "✅ API is running and healthy!" -ForegroundColor Green
    }
}
catch {
    Write-Host "❌ API is not responding at $ApiUrl" -ForegroundColor Red
    Write-Host "   Make sure to run: dotnet run" -ForegroundColor Yellow
    exit 1
}

# Test Login
Write-Host "`n🔐 Testing login..." -ForegroundColor Yellow
Write-Host "   Email: $Email" -ForegroundColor Cyan
Write-Host "   Password: ****" -ForegroundColor Cyan

$loginBody = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest `
        -Uri "$ApiUrl/api/v1/Users/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop

    $data = $response.Content | ConvertFrom-Json

    if ($data.success -eq $true) {
        Write-Host "✅ Login successful!" -ForegroundColor Green
        Write-Host "`n📊 Login Response:" -ForegroundColor Yellow
        Write-Host "   User ID: $($data.userId)" -ForegroundColor Cyan
        Write-Host "   Token Expires In: $($data.expiresIn) seconds" -ForegroundColor Cyan

        $token = $data.token

        # Save token to file
        $token | Out-File -FilePath "jwt_token.txt" -Encoding UTF8 -NoNewline
        Write-Host "`n💾 Token saved to: jwt_token.txt" -ForegroundColor Yellow

        # Display token (first and last 20 chars)
        $tokenPreview = "$($token.Substring(0, 20))...$($token.Substring($token.Length - 20))"
        Write-Host "`n🎫 JWT Token:" -ForegroundColor Yellow
        Write-Host "   $tokenPreview" -ForegroundColor Cyan

        # Instructions for Swagger
        Write-Host "`n📖 Next steps:" -ForegroundColor Green
        Write-Host "   1. Open: $ApiUrl" -ForegroundColor Cyan
        Write-Host "   2. Click 'Authorize' button (top right)" -ForegroundColor Cyan
        Write-Host "   3. Paste this in the input:" -ForegroundColor Cyan
        Write-Host "      Bearer $token" -ForegroundColor Cyan
        Write-Host "   4. Click 'Authorize' then 'Close'" -ForegroundColor Cyan
        Write-Host "   5. Now you can test protected endpoints!" -ForegroundColor Cyan

        # Test protected endpoint (optional)
        Write-Host "`n🧪 Testing protected endpoint..." -ForegroundColor Yellow
        try {
            $headers = @{
                "Authorization" = "Bearer $token"
                "Content-Type" = "application/json"
            }

            $profileResponse = Invoke-WebRequest `
                -Uri "$ApiUrl/api/v1/Users/profile" `
                -Headers $headers `
                -ErrorAction SilentlyContinue

            if ($profileResponse.StatusCode -eq 200) {
                $profileData = $profileResponse.Content | ConvertFrom-Json
                Write-Host "✅ Protected endpoint works!" -ForegroundColor Green
                Write-Host "   Response: $($profileData | ConvertTo-Json)" -ForegroundColor Cyan
            }
        }
        catch {
            Write-Host "⚠️  Protected endpoint test skipped (endpoint may not exist)" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "❌ Login failed!" -ForegroundColor Red
        Write-Host "   Message: $($data.message)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Request failed!" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n✨ Test complete!" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
