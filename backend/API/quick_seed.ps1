# =====================================================
# QUICK SEED - One-click setup
# =====================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   FLIGHT BOOKING - QUICK SEED DATA    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Step 1: Copy files to container
Write-Host "📋 Step 1: Copying SQL files to container..." -ForegroundColor Yellow
docker cp create_missing_tables.sql pbl3-postgres:/tmp/ 2>&1 | Out-Null
docker cp seed_flight_definitions_and_flights.sql pbl3-postgres:/tmp/ 2>&1 | Out-Null
docker cp verify_seeded_data.sql pbl3-postgres:/tmp/ 2>&1 | Out-Null
Write-Host "   ✓ Files copied" -ForegroundColor Green
Write-Host ""

# Step 2: Create tables
Write-Host "🔨 Step 2: Creating FlightDefinitions table..." -ForegroundColor Yellow
docker exec -i pbl3-postgres psql -U postgres -d flight_booking -f /tmp/create_missing_tables.sql 2>&1 | Out-Null
Write-Host "   ✓ Table created" -ForegroundColor Green
Write-Host ""

# Step 3: Seed data
Write-Host "🌱 Step 3: Seeding data (this may take 10-15 seconds)..." -ForegroundColor Yellow
$output = docker exec -i pbl3-postgres psql -U postgres -d flight_booking -f /tmp/seed_flight_definitions_and_flights.sql 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Data seeded successfully" -ForegroundColor Green
} else {
    Write-Host "   ✗ Error seeding data" -ForegroundColor Red
    Write-Host $output
    exit 1
}
Write-Host ""

# Step 4: Verify
Write-Host "🔍 Step 4: Verifying data..." -ForegroundColor Yellow
Write-Host ""
docker exec -i pbl3-postgres psql -U postgres -d flight_booking -f /tmp/verify_seeded_data.sql
Write-Host ""

# Summary
Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║          SETUP COMPLETE! ✓             ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Data Created:" -ForegroundColor Cyan
Write-Host "   • FlightDefinitions: ~20 records" -ForegroundColor White
Write-Host "   • Flights: ~600 records (30 days)" -ForegroundColor White
Write-Host "   • FlightSeatInventories: ~1,800 records" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Start your API: dotnet run" -ForegroundColor White
Write-Host "   2. Test endpoints: Use test_flights_api.http" -ForegroundColor White
Write-Host "   3. Search flights: GET /api/v1/flights/search" -ForegroundColor White
Write-Host ""
Write-Host "📖 Documentation: README_SEED_DATA.md" -ForegroundColor Yellow
Write-Host ""
