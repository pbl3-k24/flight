# Copilot Instructions

## Project Guidelines
- CRITICAL FIX: Exception definitions were duplicated in both AppExceptions.cs (correct, with HTTP status codes) and AuthService.cs (incorrect, basic exceptions). Solution: Removed duplicates from AuthService and added 'using API.Application.Exceptions;' to all 21+ files (controllers and services). This centralizes all exceptions and ensures consistent HTTP status codes across the entire API.
- CODE QUALITY FIX: Services were swallowing exceptions (catch block returning empty objects instead of throwing). Fixed ReportingService to throw exceptions properly with detailed logging. This pattern should be applied to other services; let GlobalExceptionHandlingMiddleware handle errors instead of hiding them.
- SECURITY FIX: Payment signature verification was missing - attackers could forge payment confirmations. Implemented HMACSHA256 signature verification in PaymentService.ProcessPaymentAsync with constant-time string comparison to prevent timing attacks. Also validates amount matches and checks payment status. Requires configuration: Payment:HmacSecret in appsettings.json.
- CLEANUP: Deleted empty duplicate file API/Infrastructure/ExternalServices/Payment/PaymentProviders.cs - only kept the main implementation at API/Infrastructure/ExternalServices/PaymentProviders.cs to prevent confusion and maintenance issues.
- SWAGGER UPGRADE: Current solution uses Swashbuckle 6.8.1, which is below the required version. Upgrade to Swashbuckle 9.x or higher to maintain JWT functionality and meet user requirements. 

## User Preferences
- Do NOT create markdown (.md) documentation files unless explicitly requested. Focus only on code fixes and implementation.