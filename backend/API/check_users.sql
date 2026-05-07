-- Kiểm tra users trong database

SELECT 
    "Id",
    "Email",
    "FullName",
    "Status",
    "IsEmailVerified",
    "CreatedAt"
FROM "Users"
ORDER BY "Id"
LIMIT 10;

-- Kiểm tra roles của user
SELECT 
    u."Email",
    r."Name" as "Role"
FROM "Users" u
LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "Roles" r ON r."Id" = ur."RoleId"
ORDER BY u."Id";
