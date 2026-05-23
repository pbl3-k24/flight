using Npgsql;

var connString = "Host=localhost;Port=5433;Database=FlightBookingDB;Username=admin;Password=SecretPassword123!";
var sql = @"
INSERT INTO ""Promotions"" (""Code"", ""DiscountType"", ""DiscountValue"", ""ValidFrom"", ""ValidTo"", ""UsageLimit"", ""UsedCount"", ""IsActive"", ""CreatedAt"", ""CreatedBy"", ""UpdatedBy"", ""IsDeleted"", ""Version"")
VALUES
('WELCOME10', 0, 10.00, NOW() - INTERVAL '1 day', NOW() + INTERVAL '90 day', 1000, 0, TRUE, NOW(), NULL, NULL, FALSE, 0),
('FLY50K', 1, 50000.00, NOW() - INTERVAL '1 day', NOW() + INTERVAL '60 day', 500, 0, TRUE, NOW(), NULL, NULL, FALSE, 0),
('SUMMER15', 0, 15.00, NOW() - INTERVAL '1 day', NOW() + INTERVAL '45 day', 300, 0, TRUE, NOW(), NULL, NULL, FALSE, 0),
('NEWUSER20', 0, 20.00, NOW() - INTERVAL '1 day', NOW() + INTERVAL '30 day', 200, 0, TRUE, NOW(), NULL, NULL, FALSE, 0),
('WEEKEND30K', 1, 30000.00, NOW() - INTERVAL '1 day', NOW() + INTERVAL '30 day', 400, 0, TRUE, NOW(), NULL, NULL, FALSE, 0)
ON CONFLICT (""Code"") DO UPDATE SET
""DiscountType"" = EXCLUDED.""DiscountType"",
""DiscountValue"" = EXCLUDED.""DiscountValue"",
""ValidFrom"" = EXCLUDED.""ValidFrom"",
""ValidTo"" = EXCLUDED.""ValidTo"",
""UsageLimit"" = EXCLUDED.""UsageLimit"",
""IsActive"" = EXCLUDED.""IsActive"",
""IsDeleted"" = FALSE;
";

await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();
await using (var cmd = new NpgsqlCommand(sql, conn))
{
    await cmd.ExecuteNonQueryAsync();
}

await using (var checkCmd = new NpgsqlCommand(@"SELECT ""Code"", ""DiscountType"", ""DiscountValue"", ""UsageLimit"", ""IsActive"" FROM ""Promotions"" WHERE ""Code"" IN ('WELCOME10','FLY50K','SUMMER15','NEWUSER20','WEEKEND30K') ORDER BY ""Code"";", conn))
await using (var reader = await checkCmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"{reader.GetString(0)} | type={reader.GetInt32(1)} | value={reader.GetDecimal(2)} | limit={(reader.IsDBNull(3) ? "null" : reader.GetInt32(3))} | active={reader.GetBoolean(4)}");
    }
}
