-- =====================================================
-- DEBUG LOGIN ISSUE
-- =====================================================

\echo '========================================'
\echo 'DEBUGGING LOGIN ISSUE'
\echo '========================================'
\echo ''

-- 1. Kiểm tra Users table có dữ liệu không
\echo '1. CHECK USERS TABLE:'
\echo '----------------------------------------'
SELECT 
    COUNT(*) as "TotalUsers",
    COUNT(CASE WHEN "Status" = 0 THEN 1 END) as "ActiveUsers",
    COUNT(CASE WHEN "Status" = 1 THEN 1 END) as "InactiveUsers",
    COUNT(CASE WHEN "IsEmailVerified" = TRUE THEN 1 END) as "VerifiedUsers"
FROM "Users";

\echo ''
\echo '2. LIST ALL USERS:'
\echo '----------------------------------------'
SELECT 
    "Id",
    "Email",
    "FullName",
    "Status",
    "IsEmailVerified",
    "CreatedAt",
    LENGTH("PasswordHash") as "PasswordHashLength"
FROM "Users"
ORDER BY "Id"
LIMIT 10;

\echo ''
\echo '3. CHECK ROLES:'
\echo '----------------------------------------'
SELECT * FROM "Roles" ORDER BY "Id";

\echo ''
\echo '4. CHECK USER-ROLE MAPPINGS:'
\echo '----------------------------------------'
SELECT 
    u."Email",
    r."Name" as "Role",
    ur."UserId",
    ur."RoleId"
FROM "Users" u
LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "Roles" r ON r."Id" = ur."RoleId"
ORDER BY u."Id";

\echo ''
\echo '5. CHECK IF TEST USERS EXIST:'
\echo '----------------------------------------'
SELECT 
    "Email",
    "FullName",
    "Status",
    "IsEmailVerified",
    CASE 
        WHEN "Status" = 0 THEN 'Active'
        WHEN "Status" = 1 THEN 'Inactive'
        WHEN "Status" = 2 THEN 'Suspended'
        ELSE 'Unknown'
    END as "StatusText"
FROM "Users"
WHERE "Email" IN ('admin@test.com', 'customer@test.com', 'test@example.com');

\echo ''
\echo '6. CHECK PASSWORD HASH FORMAT:'
\echo '----------------------------------------'
SELECT 
    "Email",
    SUBSTRING("PasswordHash", 1, 10) as "HashPrefix",
    LENGTH("PasswordHash") as "HashLength",
    CASE 
        WHEN "PasswordHash" LIKE '$2a$%' THEN 'BCrypt (correct)'
        WHEN "PasswordHash" LIKE '$2b$%' THEN 'BCrypt variant'
        ELSE 'Unknown format'
    END as "HashType"
FROM "Users"
LIMIT 5;

\echo ''
\echo '========================================'
\echo 'DIAGNOSIS:'
\echo '========================================'
\echo ''
\echo 'Possible issues:'
\echo '  1. No users in database'
\echo '  2. User Status is not 0 (Active)'
\echo '  3. Password hash format is wrong'
\echo '  4. User has no roles assigned'
\echo '  5. Email case sensitivity issue'
\echo ''
\echo 'Next steps:'
\echo '  - If no users: Run create_test_user.sql'
\echo '  - If wrong hash: Password was not hashed with BCrypt'
\echo '  - If no roles: Run seed_real_data.sql to create roles'
\echo ''
