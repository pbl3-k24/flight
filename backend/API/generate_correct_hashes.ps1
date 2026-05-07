# Generate correct BCrypt hashes using the API

Write-Host "Generating BCrypt hashes for Test@123..." -ForegroundColor Yellow
Write-Host ""

$response = Invoke-RestMethod -Uri "http://localhost:5042/api/test/hash/Test@123" -UseBasicParsing
Write-Host "Password: Test@123" -ForegroundColor Cyan
Write-Host "Hash: $($response.hash)" -ForegroundColor Green
Write-Host ""
Write-Host "Copy this hash to the SQL file!" -ForegroundColor Yellow
