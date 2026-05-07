-- Fix missing audit columns in Bookings table
-- This adds CreatedBy and UpdatedBy columns that exist in the Entity but not in DB

-- Add CreatedBy column if not exists
ALTER TABLE "Bookings" 
ADD COLUMN IF NOT EXISTS "CreatedBy" integer NULL;

-- Add UpdatedBy column if not exists
ALTER TABLE "Bookings" 
ADD COLUMN IF NOT EXISTS "UpdatedBy" integer NULL;

-- Mark migrations as applied
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Mark InitialCreate as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260506165444_InitialCreate', '9.0.0')
ON CONFLICT DO NOTHING;

-- Mark AddAuditFieldsToBooking as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260506170556_AddAuditFieldsToBooking', '9.0.0')
ON CONFLICT DO NOTHING;

-- Verify
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
