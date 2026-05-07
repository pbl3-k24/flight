-- Get the full password hash
SELECT 
    "Email",
    "PasswordHash"
FROM "Users"
WHERE "Email" = 'admin@flightbooking.vn';
