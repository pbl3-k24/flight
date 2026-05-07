-- =====================================================
-- XÓA USERS CŨ CÓ PASSWORD SAI
-- =====================================================

\echo 'Deleting old users with incorrect password hash...'

-- Xóa UserRoles trước (foreign key)
DELETE FROM "UserRoles";

-- Xóa các bảng liên quan
DELETE FROM "EmailVerificationTokens";
DELETE FROM "PasswordResetTokens";
DELETE FROM "NotificationLogs";

-- Xóa Users
DELETE FROM "Users";

\echo 'Old users deleted successfully!'
\echo ''
\echo 'Next steps:'
\echo '1. Use API to register new users: POST /api/v1/Users/register'
\echo '2. Password will be hashed correctly with BCrypt'
\echo '3. Then you can login successfully'
\echo ''
\echo 'Example:'
\echo '  POST /api/v1/Users/register'
\echo '  {'
\echo '    "email": "admin@test.com",'
\echo '    "password": "Test@123",'
\echo '    "fullName": "Admin User",'
\echo '    "phone": "0901234567"'
\echo '  }'
\echo ''
