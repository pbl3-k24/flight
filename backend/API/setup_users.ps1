# =====================================================
# SETUP USERS - Create users with BCrypt password hash
# =====================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SETUP USERS WITH BCRYPT HASH" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Copy SQL file to container
Write-Host "Copying SQL file to container..." -ForegroundColor Yellow
docker cp create_users_with_bcrypt.sql flight_postgres_db:/tmp/ 2>&1 | Out-Null
Write-Host "File copied successfully" -ForegroundColor Green
Write-Host ""

# Execute SQL
Write-Host "Creating users..." -ForegroundColor Yellow
Write-Host ""
docker exec -i flight_postgres_db psql -U admin -d FlightBookingDB -f /tmp/create_users_with_bcrypt.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  USERS CREATED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Login Credentials:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Admin:" -ForegroundColor Yellow
    Write-Host "  Email: admin@flightbooking.vn" -ForegroundColor White
    Write-Host "  Password: Test@123" -ForegroundColor White
    Write-Host ""
    Write-Host "Customers:" -ForegroundColor Yellow
    Write-Host "  Email: user1@gmail.com | Password: Test@123" -ForegroundColor White
    Write-Host "  Email: user2@gmail.com | Password: Test@123" -ForegroundColor White
    Write-Host "  Email: ducnhan@gmail.com | Password: Test@123" -ForegroundColor White
    Write-Host ""
    Write-Host "Test Login:" -ForegroundColor Cyan
    Write-Host "  POST http://localhost:5042/api/v1/Users/login" -ForegroundColor White
    Write-Host '  {"email":"admin@flightbooking.vn","password":"Test@123"}' -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "Error creating users" -ForegroundColor Red
    exit 1
}
