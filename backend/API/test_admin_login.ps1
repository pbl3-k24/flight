# Test Admin Login

$url = "http://localhost:5042/api/v1/Users/login"
$body = @{
    email = "admin@flightbooking.vn"
    password = "Admin@123456"
} | ConvertTo-Json

Write-Host "Testing admin login..." -ForegroundColor Yellow
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $url -Method POST -Body $body -ContentType "application/json"
    Write-Host "LOGIN SUCCESS!" -ForegroundColor Green
    Write-Host ""
    Write-Host "User ID: $($response.userId)" -ForegroundColor Cyan
    Write-Host "Email: $($response.email)" -ForegroundColor Cyan
    Write-Host "Full Name: $($response.fullName)" -ForegroundColor Cyan
    Write-Host "Token: $($response.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "LOGIN FAILED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.ErrorDetails.Message) {
        Write-Host ""
        Write-Host "Details:" -ForegroundColor Yellow
        Write-Host $_.ErrorDetails.Message
    }
}
