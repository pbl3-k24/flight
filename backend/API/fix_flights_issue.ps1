# =====================================================
# FIX FLIGHTS ISSUE - Xóa flights lỗi và tạo lại
# =====================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FIX FLIGHTS ISSUE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Bước 1: Xóa flights lỗi (không có FlightDefinitionId)
Write-Host "Step 1: Deleting invalid flights..." -ForegroundColor Yellow
$deleteScript = @"
-- Xóa FlightSeatInventories của flights lỗi
DELETE FROM "FlightSeatInventories"
WHERE "FlightId" IN (SELECT "Id" FROM "Flights" WHERE "FlightDefinitionId" IS NULL);

-- Xóa flights lỗi
DELETE FROM "Flights" WHERE "FlightDefinitionId" IS NULL;

-- Reset sequence
SELECT setval('"Flights_Id_seq"', COALESCE((SELECT MAX("Id") FROM "Flights"), 0) + 1, false);
"@

$deleteScript | Out-File -FilePath "temp_delete_invalid_flights.sql" -Encoding UTF8
docker cp temp_delete_invalid_flights.sql flight_postgres_db:/tmp/
docker exec -i flight_postgres_db psql -U admin -d FlightBookingDB -f /tmp/temp_delete_invalid_flights.sql | Out-Null
Remove-Item "temp_delete_invalid_flights.sql"
Write-Host "  Invalid flights deleted" -ForegroundColor Green
Write-Host ""

# Bước 2: Chạy script seed flights
Write-Host "Step 2: Seeding flights from FlightDefinitions..." -ForegroundColor Yellow
if (Test-Path "seed_flight_definitions_and_flights.sql") {
    docker cp seed_flight_definitions_and_flights.sql flight_postgres_db:/tmp/
    docker exec -i flight_postgres_db psql -U admin -d FlightBookingDB -f /tmp/seed_flight_definitions_and_flights.sql
    Write-Host "  Flights seeded successfully" -ForegroundColor Green
} else {
    Write-Host "  Warning: seed_flight_definitions_and_flights.sql not found" -ForegroundColor Yellow
    Write-Host "  Please run the seed script manually" -ForegroundColor Yellow
}
Write-Host ""

# Bước 3: Verify
Write-Host "Step 3: Verifying..." -ForegroundColor Yellow
$verifyScript = @"
-- Count flights
SELECT COUNT(*) as "TotalFlights" FROM "Flights";

-- Count flights with FlightDefinition
SELECT COUNT(*) as "FlightsWithDefinition" 
FROM "Flights" 
WHERE "FlightDefinitionId" IS NOT NULL;

-- Sample flights
SELECT 
    f."Id",
    fd."FlightNumber",
    f."DepartureTime",
    f."ArrivalTime",
    dep."Code" as "From",
    arr."Code" as "To"
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
INNER JOIN "Routes" r ON fd."RouteId" = r."Id"
INNER JOIN "Airports" dep ON r."DepartureAirportId" = dep."Id"
INNER JOIN "Airports" arr ON r."ArrivalAirportId" = arr."Id"
ORDER BY f."DepartureTime"
LIMIT 5;
"@

$verifyScript | Out-File -FilePath "temp_verify_flights.sql" -Encoding UTF8
docker cp temp_verify_flights.sql flight_postgres_db:/tmp/
Write-Host ""
docker exec -i flight_postgres_db psql -U admin -d FlightBookingDB -f /tmp/temp_verify_flights.sql
Remove-Item "temp_verify_flights.sql"

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  FLIGHTS FIXED!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
