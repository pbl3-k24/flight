-- Đánh dấu InitialCreate migration là đã apply (không chạy nó)
-- Vì database đã có sẵn tất cả bảng rồi

-- Tạo bảng migrations history nếu chưa có
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Đánh dấu InitialCreate đã apply (KHÔNG CHẠY migration này)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260506165444_InitialCreate', '9.0.0')
ON CONFLICT DO NOTHING;

-- Verify
SELECT * FROM "__EFMigrationsHistory";
