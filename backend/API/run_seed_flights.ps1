# =====================================================
# Script để seed FlightDefinitions và Flights
# =====================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SEED FLIGHT DEFINITIONS AND FLIGHTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Database connection settings
$env:PGPASSWORD = "123456"
$dbHost = "localhost"
$dbUser = "postgres"
$dbName = "flight_booking"
$sqlFile = "seed_flight_definitions_and_flights.sql"

Write-Host "Connecting to database: $dbName@$dbHost" -ForegroundColor Yellow
Write-Host ""

# Check if SQL file exists
if (-not (Test-Path $sqlFile)) {
    Write-Host "ERROR: File $sqlFile not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Executing SQL file: $sqlFile" -ForegroundColor Yellow
Write-Host ""

# Execute SQL file using docker
try {
    docker exec -i pbl3-postgres psql -U $dbUser -d $dbName -f /tmp/$sqlFile 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "SUCCESS! Data seeded successfully" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Summary:" -ForegroundColor Cyan
        Write-Host "- FlightDefinitions: ~20 records" -ForegroundColor White
        Write-Host "- Flights: ~600 records (30 days)" -ForegroundColor White
        Write-Host "- FlightSeatInventories: ~1800 records" -ForegroundColor White
    } else {
        Write-Host ""
        Write-Host "ERROR: Failed to execute SQL" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
