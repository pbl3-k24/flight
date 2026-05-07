# =====================================================
# TẠO TÀI KHOẢN ADMIN MỚI
# =====================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREATE ADMIN ACCOUNT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Bước 1: Xóa tất cả users cũ
Write-Host "Step 1: Deleting old users..." -ForegroundColor Yellow
$deleteScript = @"
DELETE FROM "UserRoles";
DELETE FROM "EmailVerificationTokens";
DELETE FROM "PasswordResetTokens"; 
DELETE FROM "NotificationLogs" WHERE "UserId" IN (SELECT "Id" FROM "Users");
DELETE FROM "Users";
ALTER SEQUENCE "Users_Id_seq" RESTART WITH 1;
"@

$deleteScript | Out-File -FilePath "temp_delete_users.sql" -Encoding UTF8
docker cp temp_delete_users.sql flight_postgres_db:/tmp/
docker exec -i flight_postgres_db psql -U admin -d FlightBookingDB -f /tmp/temp_delete_users.sql | Out-Null
Remove-Item "temp_delete_users.sql"
Write-Host "  Old users deleted" -ForegroundColor Green
Write-Host ""

# Bước 2: Đăng ký tài khoản admin qua API
Write-Host "Step 2: Registering admin account via API..." -ForegroundColor Yellow
$registerBody = @{
    email = "admin@flightbooking.vn"
    password = "Admin@123456"
    fullName = "Administrator"
    phone = "0901234567"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5042/api/v1/Users/register" -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "  Admin account created successfully!" -ForegroundColor Green
    Write-Host "  User ID: $($response.userId)" -ForegroundColor Gray
    $userId = $response.userId
} catch {
    Write-Host "  Error creating admin account:" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Bước 3: Gán role Admin
Write-Host "Step 3: Assigning Admin role..." -ForegroundColor Yellow
$assignRoleScript = @"
-- Gán role Admin cho user vừa tạo
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" = 'admin@flightbooking.vn' AND r."Name" = 'Admin'
ON CONFLICT DO NOTHING;
"@

$assignRoleScript | Out-File -FilePath "temp_assign_role.sql" -Encoding UTF8
docker cp temp_assign_role.sql flight_postgres_db:/tmp/
docker exec -i flight_postgres_db psql -U admin -d FlightBookingDB -f /tmp/temp_assign_role.sql | Out-Null
Remove-Item "temp_assign_role.sql"
Write-Host "  Admin role assigned" -ForegroundColor Green
Write-Host ""

# Bước 4: Verify
Write-Host "Step 4: Verifying..." -ForegroundColor Yellow
$verifyScript = @"
SELECT 
    u."Id",
    u."Email",
    u."FullName",
    u."Phone",
    u."Status",
    r."Name" as "Role"
FROM "Users" u
LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "Roles" r ON r."Id" = ur."RoleId"
WHERE u."Email" = 'admin@flightbooking.vn';
"@

$verifyScript | Out-File -FilePath "temp_verify.sql" -Encoding UTF8
docker cp temp_verify.sql flight_postgres_db:/tmp/
Write-Host ""
docker exec -i flight_postgres_db psql -U admin -d FlightBookingDB -f /tmp/temp_verify.sql
Remove-Item "temp_verify.sql"

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ADMIN ACCOUNT CREATED!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Login Credentials:" -ForegroundColor Cyan
Write-Host "  Email: admin@flightbooking.vn" -ForegroundColor White
Write-Host "  Password: Admin@123456" -ForegroundColor White
Write-Host ""
Write-Host "Test Login:" -ForegroundColor Cyan
Write-Host "  POST http://localhost:5042/api/v1/Users/login" -ForegroundColor White
Write-Host '  {"email":"admin@flightbooking.vn","password":"Admin@123456"}' -ForegroundColor Gray
Write-Host ""
