# Token Debug & Verification Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "JWT Token Debug & Database Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5042"

# Step 1: Check database admin roles
Write-Host "[1/4] Checking admin user in database..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/debug/check-admin-roles" -Method Get
    Write-Host "✅ Database check: $($response.StatusCode)" -ForegroundColor Green
    $result = $response.Content | ConvertFrom-Json
    Write-Host ($result | ConvertTo-Json -Depth 5) -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Check all UserRoles in database
Write-Host "[2/4] Checking all UserRoles in database..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/debug/check-all-user-roles" -Method Get
    Write-Host "✅ All UserRoles: $($response.StatusCode)" -ForegroundColor Green
    $result = $response.Content | ConvertFrom-Json
    Write-Host ($result | ConvertTo-Json -Depth 5) -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Login to get token
Write-Host "[3/4] Logging in to get token..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = "admin@flightbooking.vn"
        password = "Admin@123456"
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/users/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody

    $loginResponse = $response.Content | ConvertFrom-Json
    $token = $loginResponse.token

    Write-Host "✅ Login successful: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Token (first 50 chars): $($token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host ""

    # Decode token to see claims
    Write-Host "[4/4] Decoding token claims..." -ForegroundColor Yellow
    $parts = $token.Split('.')
    $payload = $parts[1] + ('=' * (4 - $parts[1].Length % 4))
    $decoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload)) | ConvertFrom-Json

    Write-Host "✅ Token Payload:" -ForegroundColor Green
    Write-Host ($decoded | ConvertTo-Json) -ForegroundColor Gray
    Write-Host ""

    # Check if role claim exists
    if ($decoded.psobject.properties.name -contains "role" -or $decoded.psobject.properties.name -contains "http://schemas.microsoft.com/ws/2008/06/identity/claims/role") {
        Write-Host "✅ Role claim FOUND in token" -ForegroundColor Green
    } else {
        Write-Host "❌ Role claim NOT FOUND in token - THIS IS THE PROBLEM!" -ForegroundColor Red
        Write-Host "   Available claims: $($decoded.psobject.properties.name -join ', ')" -ForegroundColor Red
    }

} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Diagnosis Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
