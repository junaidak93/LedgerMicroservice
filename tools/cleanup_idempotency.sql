-- Cleanup expired idempotency keys
DELETE FROM "IdempotencyKeys" WHERE "ExpiresAt" IS NOT NULL AND "ExpiresAt" < NOW();

-- Optionally vacuum the table (Postgres)
-- VACUUM (VERBOSE, ANALYZE) "IdempotencyKeys";
