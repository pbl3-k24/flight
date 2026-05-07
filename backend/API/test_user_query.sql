-- Test user query with roles
SELECT 
    u."Id",
    u."Email",
    u."FullName",
    u."Status",
    u."IsEmailVerified",
    SUBSTRING(u."PasswordHash", 1, 20) as "HashStart",
    r."Name" as "RoleName"
FROM "Users" u
LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "Roles" r ON r."Id" = ur."RoleId"
WHERE LOWER(u."Email") = LOWER('admin@flightbooking.vn');
