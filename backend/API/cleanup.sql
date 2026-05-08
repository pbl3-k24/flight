ALTER TABLE "Payments" 
DROP COLUMN IF EXISTS "PaymentMethod", 
DROP COLUMN IF EXISTS "TransactionId", 
DROP COLUMN IF EXISTS "PaymentGatewayResponse";
