-- Kiểm tra cấu trúc bảng Flights
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'Flights'
ORDER BY ordinal_position;
