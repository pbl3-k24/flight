#!/usr/bin/env pwsh
# Script to restart API with new changes

Write-Host "Restarting API..." -ForegroundColor Cyan

# Step 1: Stop running API processes
Write-Host "`nStopping API processes..." -ForegroundColor Yellow
$apiProcesses = Get-Process -Name "API" -ErrorAction SilentlyContinue
if ($apiProcesses) {
    $apiProcesses | ForEach-Object {
        Write-Host "Stopping process $($_.Id)..." -ForegroundColor Gray
        Stop-Process -Id $_.Id -Force
    }
    Start-Sleep -Seconds 2
    Write-Host "API processes stopped" -ForegroundColor Green
}
else {
    Write-Host "No API processes running" -ForegroundColor Gray
}

# Step 2: Build project
Write-Host "`nBuilding project..." -ForegroundColor Yellow
$buildResult = dotnet build 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful" -ForegroundColor Green
}
else {
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

# Step 3: Start API
Write-Host "`nStarting API..." -ForegroundColor Yellow
Start-Process pwsh -ArgumentList "-NoExit", "-Command", "dotnet run"
Start-Sleep -Seconds 3
Write-Host "API started" -ForegroundColor Green

Write-Host "`nAPI restarted successfully!" -ForegroundColor Green
Write-Host "API URL: http://localhost:5042" -ForegroundColor Cyan
Write-Host "Swagger: http://localhost:5042/swagger" -ForegroundColor Cyan
