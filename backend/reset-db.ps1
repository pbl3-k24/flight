#!/usr/bin/env powershell

# Flight Booking API - Database Reset Script
# Dùng để reset database và reseed data

Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Flight Booking API - Database Reset Script" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Get connection details from appsettings.json
$settingsFile = "API/appsettings.json"
if (-not (Test-Path $settingsFile)) {
    Write-Host "❌ Không tìm thấy $settingsFile" -ForegroundColor Red
    exit 1
}

# PostgreSQL Connection Details
$pgHost = "localhost"
$pgPort = "5432"
$pgUser = "admin"
$pgPassword = "SecretPassword123!"
$dbName = "FlightBookingDB"

Write-Host "📋 PostgreSQL Connection:" -ForegroundColor Green
Write-Host "  Host: $pgHost"
Write-Host "  Port: $pgPort"
Write-Host "  Database: $dbName"
Write-Host "  User: $pgUser"
Write-Host ""

# Step 1: Drop database if exists
Write-Host "🗑️  Step 1: Xóa database cũ (nếu có)..." -ForegroundColor Yellow

$env:PGPASSWORD = $pgPassword
try {
    & psql -h $pgHost -p $pgPort -U $pgUser -c "DROP DATABASE IF EXISTS ""$dbName"";" 2>$null
    Write-Host "✅ Database đã xóa hoặc không tồn tại" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Lỗi khi xóa database: $_" -ForegroundColor Yellow
    Write-Host "    Tiếp tục bằng cách tạo database mới..." -ForegroundColor Yellow
}

# Step 2: Create new database
Write-Host ""
Write-Host "📦 Step 2: Tạo database mới..." -ForegroundColor Yellow

try {
    & psql -h $pgHost -p $pgPort -U $pgUser -c "CREATE DATABASE ""$dbName"";" 2>$null
    Write-Host "✅ Database mới đã tạo" -ForegroundColor Green
} catch {
    Write-Host "❌ Lỗi tạo database: $_" -ForegroundColor Red
    exit 1
}

# Clear password from environment
Remove-Item env:PGPASSWORD -ErrorAction SilentlyContinue

# Step 3: Run migrations
Write-Host ""
Write-Host "🔄 Step 3: Chạy migrations..." -ForegroundColor Yellow

try {
    & dotnet ef database update --project API/API.csproj
    Write-Host "✅ Migrations chạy thành công" -ForegroundColor Green
} catch {
    Write-Host "❌ Lỗi chạy migrations: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Build project
Write-Host ""
Write-Host "🔨 Step 4: Build project..." -ForegroundColor Yellow

try {
    & dotnet build API/API.csproj -c Release
    Write-Host "✅ Build thành công" -ForegroundColor Green
} catch {
    Write-Host "❌ Lỗi build: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ DATABASE RESET HOÀN THÀNH!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 TEST CREDENTIALS:" -ForegroundColor Yellow
Write-Host "  Admin:"
Write-Host "    Email: admin@flightbooking.vn"
Write-Host "    Password: Admin@123456"
Write-Host ""
Write-Host "  User 1:"
Write-Host "    Email: user1@gmail.com"
Write-Host "    Password: User1@123456"
Write-Host ""
Write-Host "  User 2:"
Write-Host "    Email: user2@gmail.com"
Write-Host "    Password: User2@123456"
Write-Host ""
Write-Host "🚀 Bây giờ chạy: dotnet run --project API/API.csproj" -ForegroundColor Cyan
Write-Host ""
