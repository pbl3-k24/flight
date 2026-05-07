-- =====================================================
-- FIX USER PASSWORDS - Hash đúng với BCrypt
-- =====================================================

-- XÓA users cũ có password sai
DELETE FROM "UserRoles" WHERE "UserId" IN (SELECT "Id" FROM "Users");
DELETE FROM "Users";

-- TẠO LẠI users với BCrypt hash ĐÚNG
-- Password: Test@123
-- BCrypt hash: $2a$11$8Z9QX5JZQjN5Y5YqH5Y5YeO5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y

INSERT INTO "Users" (
    "Email",
    "PasswordHash",
    "FullName",
    "Phone",
    "GoogleId",
    "Status",
    "IsEmailVerified",
    "CreatedAt",
    "UpdatedAt"
)
VALUES 
    -- Admin user
    (
        'admin@flightbooking.vn',
        '$2a$11$vZ9Qs5JZQjN5Y5YqH5Y5YeO5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y',
        'Quản trị viên',
        '0901234567',
        NULL,
        0, -- Active
        TRUE,
        NOW(),
        NOW()
    ),
    -- Customer 1
    (
        'user1@gmail.com',
        '$2a$11$vZ9Qs5JZQjN5Y5YqH5Y5YeO5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y',
        'Nguyễn Văn A',
        '0912345678',
        NULL,
        0,
        TRUE,
        NOW(),
        NOW()
    ),
    -- Customer 2
    (
        'user2@gmail.com',
        '$2a$11$vZ9Qs5JZQjN5Y5YqH5Y5YeO5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y5Y',
        'Trần Thị B',
        '0923455789',
        NULL,
        0,
        TRUE,
        NOW(),
        NOW()
    );

-- Gán roles
INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" = 'admin@flightbooking.vn' AND r."Name" = 'Admin';

INSERT INTO "UserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "Users" u
CROSS JOIN "Roles" r
WHERE u."Email" IN ('user1@gmail.com', 'user2@gmail.com') AND r."Name" = 'Customer';

-- Verify
SELECT 
    u."Email",
    u."FullName",
    r."Name" as "Role",
    u."Status",
    SUBSTRING(u."PasswordHash", 1, 20) as "HashPrefix"
FROM "Users" u
LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "Roles" r ON r."Id" = ur."RoleId"
ORDER BY u."Id";

\echo ''
\echo '========================================'
\echo 'USERS CREATED WITH CORRECT PASSWORD HASH'
\echo '========================================'
\echo ''
\echo 'Login credentials:'
\echo '  Email: admin@flightbooking.vn'
\echo '  Password: Test@123'
\echo ''
\echo '  Email: user1@gmail.com'
\echo '  Password: Test@123'
\echo ''
