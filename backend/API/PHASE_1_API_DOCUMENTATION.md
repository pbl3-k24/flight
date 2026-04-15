# Phase 1 API Documentation

## Authentication & User Management API

### Base URL
```
https://localhost:7001/api/v1/users
```

---

## Endpoints

### 1. Register User
**POST** `/register`

Register a new user account.

#### Request Body
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123",
  "fullName": "John Doe",
  "phone": "+1234567890"
}
```

#### Response (201 Created)
```json
{
  "userId": 1,
  "email": "user@example.com",
  "fullName": "John Doe",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-16T10:30:00Z"
}
```

#### Validation Rules
- Email must be valid format (xxx@xxx.xxx)
- Email must not already exist
- Password must be at least 8 characters
- Password must contain uppercase letter
- Password must contain digit

#### Error Responses
```json
// 400 Bad Request - Invalid email format
{
  "message": "Invalid email format"
}

// 400 Bad Request - Email already registered
{
  "message": "Email already registered"
}

// 400 Bad Request - Weak password
{
  "message": "Password must contain at least one uppercase letter"
}
```

---

### 2. Login
**POST** `/login`

Authenticate user and receive JWT token.

#### Request Body
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123"
}
```

#### Response (200 OK)
```json
{
  "userId": 1,
  "email": "user@example.com",
  "fullName": "John Doe",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-16T10:30:00Z"
}
```

#### Error Responses
```json
// 400 Bad Request - Invalid credentials
{
  "message": "Invalid email or password"
}

// 400 Bad Request - Account not active
{
  "message": "Account is not active"
}
```

---

### 3. Verify Email
**POST** `/verify-email`

Verify user's email address using verification code received in email.

#### Query Parameters
- `code` (required) - Email verification code from email

#### Example
```
POST /api/v1/users/verify-email?code=a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
```

#### Headers
```
Authorization: Bearer <JWT_TOKEN>
```

#### Response (200 OK)
```json
{
  "message": "Email verified successfully"
}
```

#### Error Responses
```json
// 400 Bad Request - Invalid code
{
  "message": "Invalid verification code"
}

// 400 Bad Request - Expired code
{
  "message": "Verification code has expired"
}

// 401 Unauthorized - Not authenticated
{
  "message": "User not authenticated"
}
```

---

### 4. Change Password
**POST** `/change-password`

Change password for authenticated user.

#### Headers
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

#### Request Body
```json
{
  "oldPassword": "CurrentPassword123",
  "newPassword": "NewSecurePassword456"
}
```

#### Response (200 OK)
```json
{
  "message": "Password changed successfully"
}
```

#### Validation Rules
- Old password must match current password
- New password must be at least 8 characters
- New password must contain uppercase letter
- New password must contain digit
- New password cannot be same as old password

#### Error Responses
```json
// 400 Bad Request - Wrong old password
{
  "message": "Old password is incorrect"
}

// 400 Bad Request - Weak new password
{
  "message": "Password must be at least 8 characters long"
}

// 401 Unauthorized - Not authenticated
{
  "message": "Invalid user context"
}
```

---

### 5. Forgot Password
**POST** `/forgot-password`

Request password reset email. Sends reset link to user's email.

#### Request Body
```json
{
  "email": "user@example.com"
}
```

#### Response (200 OK)
```json
{
  "message": "If an account with that email exists, a password reset link has been sent"
}
```

#### Security Note
This endpoint always returns 200 OK regardless of whether the email exists, to prevent email enumeration attacks.

#### Email Content
User will receive email with:
- Reset link: `https://localhost:7001/auth/reset-password?code=RESET_CODE`
- Link expires in 1 hour
- Instructions to ignore if not requested

---

### 6. Reset Password
**POST** `/reset-password`

Reset password using code from reset email.

#### Request Body
```json
{
  "code": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
  "newPassword": "NewSecurePassword789"
}
```

#### Response (200 OK)
```json
{
  "message": "Password reset successfully"
}
```

#### Validation Rules
- Code must be valid and not expired (1 hour)
- New password must be at least 8 characters
- New password must contain uppercase letter
- New password must contain digit

#### Error Responses
```json
// 400 Bad Request - Invalid code
{
  "message": "Invalid reset code"
}

// 400 Bad Request - Expired code
{
  "message": "Reset code has expired"
}

// 400 Bad Request - Weak password
{
  "message": "Password must contain at least one digit"
}
```

---

## Authentication

All protected endpoints require JWT token in Authorization header:

```
Authorization: Bearer <JWT_TOKEN>
```

### JWT Token Claims
```json
{
  "sub": "1",                                    // User ID
  "email": "user@example.com",
  "name": "John Doe",
  "iat": 1705329000,                             // Issued at
  "exp": 1705415400,                             // Expires at
  "iss": "flight-booking-api",                   // Issuer
  "aud": "flight-booking-app"                    // Audience
}
```

### Token Expiration
- Default: 24 hours
- Configurable via: `Jwt:ExpirationHours` in appsettings.json

---

## Email Notifications

### 1. Verification Email
Sent immediately after registration.
- Contains verification link
- Valid for 24 hours
- Must be verified before login

### 2. Password Reset Email
Sent when user requests password reset.
- Contains reset link
- Valid for 1 hour
- One-time use only

### 3. Password Changed Confirmation
Sent after successful password change.
- Confirms password change
- Contains timestamp
- Contact support if not requested

---

## Error Handling

### HTTP Status Codes
- `200 OK` - Successful request
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid input or validation error
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Insufficient permissions
- `500 Internal Server Error` - Server error

### Error Response Format
```json
{
  "message": "Error description"
}
```

### Common Error Scenarios

| Scenario | Status | Message |
|----------|--------|---------|
| Invalid email format | 400 | Invalid email format |
| Email already exists | 400 | Email already registered |
| Weak password | 400 | Password must be at least 8 characters long |
| Invalid credentials | 400 | Invalid email or password |
| Account not active | 400 | Account is not active |
| Expired token | 400 | Verification code has expired |
| Invalid old password | 400 | Old password is incorrect |
| Not authenticated | 401 | User not authenticated |
| Server error | 500 | An error occurred during [operation] |

---

## Examples

### Complete Registration Flow

1. **Register**
```bash
curl -X POST https://localhost:7001/api/v1/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123",
    "fullName": "John Doe",
    "phone": "+1234567890"
  }'
```

Response:
```json
{
  "userId": 1,
  "email": "john@example.com",
  "fullName": "John Doe",
  "token": "eyJhbGc...",
  "expiresAt": "2024-01-16T10:30:00Z"
}
```

2. **Check Email** (User checks email for verification link)

3. **Verify Email**
```bash
curl -X POST https://localhost:7001/api/v1/users/verify-email?code=ABC123... \
  -H "Authorization: Bearer eyJhbGc..."
```

4. **Now Ready to Login**

---

### Complete Password Reset Flow

1. **Request Reset**
```bash
curl -X POST https://localhost:7001/api/v1/users/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email": "john@example.com"}'
```

2. **User Checks Email** (Gets reset link with code)

3. **Reset Password**
```bash
curl -X POST https://localhost:7001/api/v1/users/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ABC123...",
    "newPassword": "NewPass456"
  }'
```

4. **Login with New Password**

---

## Configuration

### appsettings.json Configuration

```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-chars",
    "Issuer": "flight-booking-api",
    "Audience": "flight-booking-app",
    "ExpirationHours": 24
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@flightbooking.com"
  },
  "AppSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

### Environment Variables
You can also use environment variables:
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__ExpirationHours`
- `Smtp__Host`
- `Smtp__Port`
- `Smtp__Username`
- `Smtp__Password`
- `Smtp__FromEmail`
- `AppSettings__BaseUrl`

---

## Security Best Practices

1. **Never expose JWT tokens in logs**
2. **Always use HTTPS in production**
3. **Store JWT secret securely** (use Azure Key Vault, AWS Secrets, etc.)
4. **Use environment-specific SMTP credentials**
5. **Implement rate limiting** on authentication endpoints
6. **Monitor for suspicious login attempts**
7. **Use secure password hashing** (PBKDF2 with 10,000 iterations)
8. **Validate all inputs** on server side
9. **Never send sensitive data** in response (e.g., password hash)
10. **Implement logout** by blacklisting tokens (for future implementation)

---

## Future Enhancements

- [ ] Two-factor authentication (2FA)
- [ ] Social login (Google, Facebook, GitHub)
- [ ] Token refresh mechanism
- [ ] Account lockout after failed attempts
- [ ] Email/SMS notifications for security events
- [ ] User profile management
- [ ] Multi-language support for emails
- [ ] Passwordless authentication

---

**Last Updated**: January 2024
**API Version**: 1.0
**Status**: ✅ Complete
