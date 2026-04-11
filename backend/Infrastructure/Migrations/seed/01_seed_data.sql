-- ============================================================
-- Seed data for Domestic Flight Ticketing System
-- Vietnamese domestic routes and airports
-- ============================================================

-- Default email templates
INSERT INTO "EmailTemplates" ("Id","TemplateKey","Name","Subject","HtmlBody","IsActive") VALUES
(gen_random_uuid(), 'otp_email_verification', 'Email Verification OTP',
 'Mã xác thực email của bạn - {{Code}}',
 '<h2>Xác thực email</h2><p>Mã OTP của bạn là: <strong>{{Code}}</strong></p><p>Mã có hiệu lực trong {{ExpiryMinutes}} phút.</p>',
 true),
(gen_random_uuid(), 'otp_password_reset', 'Password Reset OTP',
 'Đặt lại mật khẩu - Mã xác thực {{Code}}',
 '<h2>Đặt lại mật khẩu</h2><p>Mã OTP của bạn là: <strong>{{Code}}</strong></p><p>Mã có hiệu lực trong {{ExpiryMinutes}} phút.</p>',
 true),
(gen_random_uuid(), 'otp_generic', 'Generic OTP',
 'Mã xác thực - {{Code}}',
 '<p>Mã OTP của bạn là: <strong>{{Code}}</strong></p>',
 true),
(gen_random_uuid(), 'booking_confirmed', 'Booking Confirmed',
 'Đặt vé thành công - Mã đặt chỗ {{BookingCode}}',
 '<h2>Đặt vé thành công!</h2><p>Mã đặt chỗ của bạn: <strong>{{BookingCode}}</strong></p><p>Tổng tiền: {{TotalAmount}} VND</p>',
 true),
(gen_random_uuid(), 'flight_changed', 'Flight Changed',
 'Thông báo thay đổi chuyến bay - {{BookingCode}}',
 '<h2>Thay đổi chuyến bay</h2><p>Chuyến bay trong đặt chỗ {{BookingCode}} có thay đổi: {{ChangeType}}</p>',
 true),
(gen_random_uuid(), 'refund_processed', 'Refund Processed',
 'Hoàn tiền - {{Status}}',
 '<h2>Cập nhật hoàn tiền</h2><p>Yêu cầu hoàn tiền của bạn đã được {{Status}}.</p><p>Số tiền: {{Amount}} VND</p>',
 true),
(gen_random_uuid(), 'ticket_issued', 'Ticket Issued',
 'Vé điện tử của bạn - {{TicketNumber}}',
 '<h2>Vé điện tử</h2><p>Số vé: <strong>{{TicketNumber}}</strong></p><p>Chuyến bay: {{FlightNumber}}</p>',
 true);

-- Vietnamese domestic airports
INSERT INTO "Airports" ("Id","Code","Name","City","Country","IsActive") VALUES
(gen_random_uuid(), 'HAN', 'Nội Bài', 'Hà Nội', 'VN', true),
(gen_random_uuid(), 'SGN', 'Tân Sơn Nhất', 'Hồ Chí Minh', 'VN', true),
(gen_random_uuid(), 'DAD', 'Đà Nẵng', 'Đà Nẵng', 'VN', true),
(gen_random_uuid(), 'PQC', 'Phú Quốc', 'Phú Quốc', 'VN', true),
(gen_random_uuid(), 'CXR', 'Cam Ranh', 'Nha Trang', 'VN', true),
(gen_random_uuid(), 'VCA', 'Cần Thơ', 'Cần Thơ', 'VN', true),
(gen_random_uuid(), 'BMV', 'Buôn Ma Thuột', 'Buôn Ma Thuột', 'VN', true),
(gen_random_uuid(), 'HUI', 'Phú Bài', 'Huế', 'VN', true),
(gen_random_uuid(), 'VCL', 'Chu Lai', 'Quảng Nam', 'VN', true),
(gen_random_uuid(), 'VDH', 'Đồng Hới', 'Quảng Bình', 'VN', true),
(gen_random_uuid(), 'UIH', 'Phù Cát', 'Quy Nhơn', 'VN', true),
(gen_random_uuid(), 'DLI', 'Liên Khương', 'Đà Lạt', 'VN', true),
(gen_random_uuid(), 'VKG', 'Rạch Giá', 'Rạch Giá', 'VN', true),
(gen_random_uuid(), 'THD', 'Thọ Xuân', 'Thanh Hóa', 'VN', true),
(gen_random_uuid(), 'VII', 'Vinh', 'Nghệ An', 'VN', true);

-- Fare classes (shared across airlines)
INSERT INTO "FareClasses" ("Id","Code","Name","CheckedBaggageKg","CabinBaggageKg","IsRefundable","RefundFeePercent","IsChangeable","ChangeFee","IsActive") VALUES
(gen_random_uuid(), 'PROMO', 'Giá khuyến mãi', 0,  7, false, 1.00, false, 0,       true),
(gen_random_uuid(), 'ECO',   'Phổ thông',      20, 7, true,  0.20, true,  330000,  true),
(gen_random_uuid(), 'ECO+',  'Phổ thông Plus', 30, 7, true,  0.15, true,  220000,  true),
(gen_random_uuid(), 'BUS',   'Thương gia',     40, 7, true,  0.10, true,  0,       true);
