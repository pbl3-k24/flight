-- =====================================================
-- TẠO USER TEST ĐỂ LOGIN
-- =====================================================

-- Password sẽ là: "Test@123"
-- Hash này được tạo bằng BCrypt với cost factor 12

DO $$
DECLARE
    v_user_id INTEGER;
    v_admin_role_id INTEGER;
    v_customer_role_id INTEGER;
BEGIN
    -- Lấy role IDs
    SELECT "Id" INTO v_admin_role_id FROM "Roles" WHERE "Name" = 'Admin' LIMIT 1;
    SELECT "Id" INTO v_customer_role_id FROM "Roles" WHERE "Name" = 'Customer' LIMIT 1;
    
    -- Tạo Admin user
    INSERT INTO "Users" (
        "Email",
        "PasswordHash",
        "FullName",
        "Phone",
        "Status",
        "IsEmailVerified",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        'admin@test.com',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u', -- Test@123
        'Admin User',
        '0123456789',
        1, -- Active
        TRUE,
        NOW(),
        NOW()
    )
    ON CONFLICT ("Email") DO NOTHING
    RETURNING "Id" INTO v_user_id;
    
    -- Gán role Admin
    IF v_user_id IS NOT NULL AND v_admin_role_id IS NOT NULL THEN
        INSERT INTO "UserRoles" ("UserId", "RoleId")
        VALUES (v_user_id, v_admin_role_id)
        ON CONFLICT DO NOTHING;
    END IF;
    
    -- Tạo Customer user
    INSERT INTO "Users" (
        "Email",
        "PasswordHash",
        "FullName",
        "Phone",
        "Status",
        "IsEmailVerified",
        "CreatedAt",
        "UpdatedAt"
    )
    VALUES (
        'customer@test.com',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIq.Zu3u6u', -- Test@123
        'Customer User',
        '0987654321',
        1, -- Active
        TRUE,
        NOW(),
        NOW()
    )
    ON CONFLICT ("Email") DO NOTHING
    RETURNING "Id" INTO v_user_id;
    
    -- Gán role Customer
    IF v_user_id IS NOT NULL AND v_customer_role_id IS NOT NULL THEN
        INSERT INTO "UserRoles" ("UserId", "RoleId")
        VALUES (v_user_id, v_customer_role_id)
        ON CONFLICT DO NOTHING;
    END IF;
    
    RAISE NOTICE 'Test users created successfully';
END $$;

-- Verify
SELECT 
    u."Email",
    u."FullName",
    u."Status",
    u."IsEmailVerified",
    r."Name" as "Role"
FROM "Users" u
LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "Roles" r ON r."Id" = ur."RoleId"
WHERE u."Email" IN ('admin@test.com', 'customer@test.com')
ORDER BY u."Email";

-- =====================================================
-- THÔNG TIN ĐĂNG NHẬP:
-- =====================================================
-- Email: admin@test.com
-- Password: Test@123
-- Role: Admin
--
-- Email: customer@test.com
-- Password: Test@123
-- Role: Customer
-- =====================================================
