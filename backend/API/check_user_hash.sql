-- Check user password hash format
SELECT 
    "Id",
    "Email",
    "FullName",
    SUBSTRING("PasswordHash", 1, 10) as "HashPrefix",
    LENGTH("PasswordHash") as "HashLength",
    CASE 
        WHEN "PasswordHash" LIKE '$2a$%' THEN 'BCrypt'
        WHEN "PasswordHash" LIKE '$2b$%' THEN 'BCrypt'
        WHEN "PasswordHash" LIKE '$2y$%' THEN 'BCrypt'
        ELSE 'Unknown/Plain'
    END as "HashType"
FROM "Users"
WHERE "Email" = 'admin@flightbooking.vn';
