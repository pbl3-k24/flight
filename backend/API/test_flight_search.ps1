# Test Flight Search API

Write-Host "Testing Flight Search API..." -ForegroundColor Yellow
Write-Host ""

# Test 1: Search SGN -> HAN today
Write-Host "Test 1: Search SGN -> HAN (today)..." -ForegroundColor Cyan
$url = "http://localhost:5042/api/v1/Flights/search?departureAirportId=1&arrivalAirportId=2&departureDate=2026-05-07&passengerCount=1"

try {
    $response = Invoke-RestMethod -Uri $url -Method GET -UseBasicParsing
    Write-Host "  Found $($response.Count) flights" -ForegroundColor Green
    if ($response.Count -gt 0) {
        Write-Host "  Sample flight:" -ForegroundColor Gray
        $flight = $response[0]
        Write-Host "    Flight: $($flight.flightNumber)" -ForegroundColor White
        Write-Host "    Route: $($flight.departureAirport) -> $($flight.arrivalAirport)" -ForegroundColor White
        Write-Host "    Departure: $($flight.departureTime)" -ForegroundColor White
        Write-Host "    Arrival: $($flight.arrivalTime)" -ForegroundColor White
    }
} catch {
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Test completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
