-- =====================================================
-- TẠO USERS VỚI BCRYPT PASSWORD HASH
-- Password: Test@123 (cho tất cả users)
-- =====================================================

-- Xóa dữ liệu cũ
DELETE FROM "UserRoles";
DELETE FROM "EmailVerificationTokens";
DELETE FROM "PasswordResetTokens"; 
DELETE FROM "NotificationLogs" WHERE "UserId" IN (SELECT "Id" FROM "Users");
DELETE FROM "Users";

-- Reset sequence
ALTER SEQUENCE "Users_Id_seq" RESTART WITH 1;

-- =====================================================
-- INSERT USERS với BCrypt hash
-- BCrypt hash được tạo từ password "Test@123" với cost factor 11
-- =====================================================

INSERT INTO "Users" (
    "Email",
    "PasswordHash",
    "FullName",
    "Phone",
    "GoogleId",
    "Status",
    "IsEmailVerified",
    "FailedLoginAttempts",
    "IsTwoFactorEnabled",
    "PhoneNumberVerified",
    "MarketingOptIn",
    "NewsletterSubscription",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted",
    "Version"
)
VALUES 
    -- Admin User
    (
        'admin@flightbooking.vn',
        '$2a$11$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u',
        'Quản trị viên',
        '0901234567',
        NULL,
        0, -- Active
        TRUE,
        0,
        FALSE,
        FALSE,
        FALSE,
        FALSE,
        NOW(),
        NOW(),
        FALSE,
        0
    ),
    -- Customer 1
    (
        'user1@gmail.com',
        '$2a$11$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u',
        'Nguyễn Văn A',
        '0912345678',
        NULL,
        0,
        TRUE,
        0,
        FALSE,
        FALSE,
        FALSE,
        FALSE,
        NOW(),
        NOW(),
        FALSE,
        0
    ),
    -- Customer 2
    (
        'user2@gmail.com',
        '$2a$11$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u',
        'Trần Thị B',
        '0923455789',
        NULL,
        0,
        TRUE,
        0,
        FALSE,
        FALSE,
        FALSE,
        FALSE,
        NOW(),
        NOW(),
        FALSE,
        0
    ),
    -- Customer 3
    (
        'ducnhan@gmail.com',
        '$2a$11$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u',
        'Đức Nhân',
        '123456',
        NULL,
        0,
        TRUE,
        0,
        FALSE,
        FALSE,
        FALSE,
        FALSE,
        NOW(),
        NOW(),
        FALSE,
        0
    );

-- =====================================================
-- GÁN ROLES
-- =====================================================

-- Admin role
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" = 'admin@flightbooking.vn' AND r."Name" = 'Admin';

-- Customer roles
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" IN ('user1@gmail.com', 'user2@gmail.com', 'ducnhan@gmail.com') 
AND r."Name" = 'Customer';

-- =====================================================
-- VERIFY
-- =====================================================

\echo ''
\echo '========================================'
\echo 'USERS CREATED SUCCESSFULLY'
\echo '========================================'
\echo ''

SELECT 
    u."Id",
    u."Email",
    u."FullName",
    u."Phone",
    u."Status",
    u."IsEmailVerified",
    r."Name" as "Role",
    SUBSTRING(u."PasswordHash", 1, 30) || '...' as "PasswordHash"
FROM "Users" u
LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "Roles" r ON r."Id" = ur."RoleId"
ORDER BY u."Id";

\echo ''
\echo '========================================'
\echo 'LOGIN CREDENTIALS'
\echo '========================================'
\echo ''
\echo 'Admin:'
\echo '  Email: admin@flightbooking.vn'
\echo '  Password: Test@123'
\echo ''
\echo 'Customer 1:'
\echo '  Email: user1@gmail.com'
\echo '  Password: Test@123'
\echo ''
\echo 'Customer 2:'
\echo '  Email: user2@gmail.com'
\echo '  Password: Test@123'
\echo ''
\echo 'Customer 3:'
\echo '  Email: ducnhan@gmail.com'
\echo '  Password: Test@123'
\echo ''
\echo '========================================'
\echo 'Bây giờ bạn có thể login với các tài khoản trên!'
\echo '========================================'
\echo ''
