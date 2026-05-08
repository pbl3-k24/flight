CREATE EXTENSION IF NOT EXISTS pgcrypto;
UPDATE "Users" SET "PasswordHash" = crypt('Admin@123456', gen_salt('bf', 11)) WHERE "Email" = 'admin@flightbooking.vn';
SELECT "PasswordHash" FROM "Users" WHERE "Email" = 'admin@flightbooking.vn';
