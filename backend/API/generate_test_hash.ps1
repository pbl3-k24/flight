# Generate BCrypt hash for Test@123

$code = @"
using System;
using BCrypt.Net;

var password = "Test@123";
var hash = BCrypt.HashPassword(password, 11);
Console.WriteLine("Password: " + password);
Console.WriteLine("Generated Hash: " + hash);
Console.WriteLine("");

// Test verification
var isValid = BCrypt.Verify(password, hash);
Console.WriteLine("Self-verification: " + isValid);
Console.WriteLine("");

// Test against the hash in database
var dbHash = "`$2a`$11`$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u";
var isValidDb = BCrypt.Verify(password, dbHash);
Console.WriteLine("Database hash verification: " + isValidDb);
"@

Write-Host "Generating BCrypt hash..." -ForegroundColor Yellow
Write-Host ""
dotnet script eval "$code"
