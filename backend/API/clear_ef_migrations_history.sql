-- Clear EF Migrations History to prevent old migrations from running
-- This forces EF Core to treat the current database schema as the baseline

BEGIN;

-- Backup current migrations (optional)
SELECT * FROM "__EFMigrationsHistory";

-- Clear all migration history
DELETE FROM "__EFMigrationsHistory";

-- Verify
SELECT COUNT(*) as "RemainingMigrations" FROM "__EFMigrationsHistory";

COMMIT;

-- Note: After running this, EF Core will not try to apply any migrations
-- The current database schema will be treated as the baseline
