-- Script để cập nhật thời gian khởi hành của các chuyến bay
-- Chạy script này khi dữ liệu test đã cũ

-- Cập nhật tất cả các chuyến bay có DepartureTime trong quá khứ
-- Đẩy chúng về ngày mai cùng giờ
UPDATE "Flights"
SET 
    "DepartureTime" = (CURRENT_TIMESTAMP AT TIME ZONE 'UTC' + INTERVAL '1 day')::date + 
                      ("DepartureTime"::time),
    "ArrivalTime" = (CURRENT_TIMESTAMP AT TIME ZONE 'UTC' + INTERVAL '1 day')::date + 
                    ("ArrivalTime"::time)
WHERE "DepartureTime" < (CURRENT_TIMESTAMP AT TIME ZONE 'UTC');

-- Kiểm tra kết quả
SELECT 
    "Id",
    "FlightNumber",
    "DepartureTime",
    "ArrivalTime",
    "Status"
FROM "Flights"
ORDER BY "DepartureTime";
