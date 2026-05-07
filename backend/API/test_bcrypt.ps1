# Test BCrypt verification

Write-Host "Testing BCrypt password verification..." -ForegroundColor Yellow
Write-Host ""

$password = "Test@123"
$hash = "`$2a`$11`$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u"

Write-Host "Password: $password" -ForegroundColor Cyan
Write-Host "Hash: $hash" -ForegroundColor Cyan
Write-Host ""

# Create a simple C# test
$code = @"
using System;
using BCrypt.Net;

public class BcryptTest {
    public static void Main() {
        string password = "Test@123";
        string hash = "`$2a`$11`$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u";
        
        Console.WriteLine("Password: " + password);
        Console.WriteLine("Hash: " + hash);
        Console.WriteLine("");
        
        try {
            bool result = BCrypt.Verify(password, hash);
            Console.WriteLine("Verification Result: " + result);
            
            if (result) {
                Console.WriteLine("✓ BCrypt verification PASSED!");
            } else {
                Console.WriteLine("✗ BCrypt verification FAILED!");
            }
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
"@

$code | Out-File -FilePath "BcryptTest.cs" -Encoding UTF8

Write-Host "Compiling test..." -ForegroundColor Yellow
dotnet new console -n BcryptTestApp -o BcryptTestTemp --force | Out-Null
Copy-Item "BcryptTest.cs" "BcryptTestTemp/Program.cs" -Force
Set-Location "BcryptTestTemp"
dotnet add package BCrypt.Net-Next | Out-Null
dotnet run
Set-Location ..
Remove-Item -Recurse -Force "BcryptTestTemp"
Remove-Item "BcryptTest.cs"
