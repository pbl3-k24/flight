-- Reconcile EF migration history for drifted environments.
-- Safe to run multiple times.

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260518104500_AddSavedPassengers'
  ) AND EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'SavedPassengers'
  ) THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260518104500_AddSavedPassengers', '10.0.5');
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260518113000_AddPromotionMaxDiscountAmount'
  ) AND EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name = 'Promotions'
      AND column_name = 'MaxDiscountAmount'
  ) THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260518113000_AddPromotionMaxDiscountAmount', '10.0.5');
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260518120000_UpdatePaymentStatusConstraintForPendingRefund'
  ) THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260518120000_UpdatePaymentStatusConstraintForPendingRefund', '10.0.5');
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260518140000_AddBookingLegsAndLegPassengers'
  ) AND EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'BookingLegs'
  ) AND EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'BookingLegPassengers'
  ) THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260518140000_AddBookingLegsAndLegPassengers', '10.0.5');
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260518143000_AllowDuplicatePassengerServiceAcrossLegs'
  ) AND EXISTS (
    SELECT 1 FROM pg_indexes
    WHERE schemaname = 'public'
      AND tablename = 'BookingServices'
      AND indexname = 'IX_BookingServices_BookingPassengerId_AdditionalServiceId'
  ) THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260518143000_AllowDuplicatePassengerServiceAcrossLegs', '10.0.5');
  END IF;
END $$;
