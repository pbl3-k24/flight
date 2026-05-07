# Script to apply EF migrations automatically
# This handles the case where database already exists

Write-Host "=== EF Core Migration Tool ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Mark InitialCreate as applied (if database already exists)
Write-Host "Step 1: Marking InitialCreate migration as applied..." -ForegroundColor Yellow

$sql = @"
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260506165444_InitialCreate', '9.0.0')
ON CONFLICT DO NOTHING;
"@

# Save SQL to temp file
$sql | Out-File -FilePath "temp_mark_migration.sql" -Encoding UTF8

# Try to run with psql
if (Get-Command psql -ErrorAction SilentlyContinue) {
    $env:PGPASSWORD = "123456"
    psql -h localhost -U postgres -d flight_booking -f temp_mark_migration.sql 2>&1 | Out-Null
    Write-Host "✓ InitialCreate marked as applied" -ForegroundColor Green
} else {
    Write-Host "⚠ Could not find psql. Please run this SQL manually:" -ForegroundColor Yellow
    Write-Host $sql
    Write-Host ""
    Write-Host "Press Enter to continue..." -ForegroundColor Yellow
    Read-Host
}

# Clean up
Remove-Item "temp_mark_migration.sql" -ErrorAction SilentlyContinue

# Step 2: Apply pending migrations
Write-Host ""
Write-Host "Step 2: Applying pending migrations..." -ForegroundColor Yellow
dotnet ef database update

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ All migrations applied successfully!" -ForegroundColor Green
} else {
    Write-Host "✗ Migration failed. Check errors above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Migration Complete ===" -ForegroundColor Cyan
Write-Host "You can now run: dotnet run" -ForegroundColor Green
