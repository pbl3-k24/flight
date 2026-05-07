-- Tạo các bảng còn thiếu cho Flight Template System

-- 1. FlightScheduleTemplates
CREATE TABLE IF NOT EXISTS "FlightScheduleTemplates" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(1000) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- 2. FlightTemplateDetails
CREATE TABLE IF NOT EXISTS "FlightTemplateDetails" (
    "Id" SERIAL PRIMARY KEY,
    "TemplateId" INTEGER NOT NULL,
    "FlightNumber" VARCHAR(20) NOT NULL,
    "RouteId" INTEGER NOT NULL,
    "AircraftId" INTEGER NOT NULL,
    "DepartureTime" TIME NOT NULL,
    "ArrivalTime" TIME NOT NULL,
    "ArrivalOffsetDays" INTEGER NOT NULL DEFAULT 0,
    "OperatingDays" INTEGER NOT NULL DEFAULT 127,
    
    CONSTRAINT "FK_FlightTemplateDetails_FlightScheduleTemplates" 
        FOREIGN KEY ("TemplateId") REFERENCES "FlightScheduleTemplates"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_FlightTemplateDetails_Routes" 
        FOREIGN KEY ("RouteId") REFERENCES "Routes"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FlightTemplateDetails_Aircraft" 
        FOREIGN KEY ("AircraftId") REFERENCES "Aircraft"("Id") ON DELETE RESTRICT
);

-- 3. FlightDefinitions
CREATE TABLE IF NOT EXISTS "FlightDefinitions" (
    "Id" SERIAL PRIMARY KEY,
    "FlightNumber" VARCHAR(20) NOT NULL,
    "RouteId" INTEGER NOT NULL,
    "DefaultAircraftId" INTEGER NOT NULL,
    "DepartureTime" TIME NOT NULL,
    "ArrivalTime" TIME NOT NULL,
    "ArrivalOffsetDays" INTEGER NOT NULL DEFAULT 0,
    "OperatingDays" INTEGER NOT NULL DEFAULT 127,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    
    CONSTRAINT "FK_FlightDefinitions_Routes" 
        FOREIGN KEY ("RouteId") REFERENCES "Routes"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FlightDefinitions_Aircraft" 
        FOREIGN KEY ("DefaultAircraftId") REFERENCES "Aircraft"("Id") ON DELETE RESTRICT,
    CONSTRAINT "UQ_FlightDefinitions_FlightNumber" 
        UNIQUE ("FlightNumber")
);

-- Tạo indexes
CREATE INDEX IF NOT EXISTS "IX_FlightTemplateDetails_TemplateId" ON "FlightTemplateDetails"("TemplateId");
CREATE INDEX IF NOT EXISTS "IX_FlightTemplateDetails_RouteId" ON "FlightTemplateDetails"("RouteId");
CREATE INDEX IF NOT EXISTS "IX_FlightTemplateDetails_AircraftId" ON "FlightTemplateDetails"("AircraftId");
CREATE INDEX IF NOT EXISTS "IX_FlightDefinitions_RouteId" ON "FlightDefinitions"("RouteId");
CREATE INDEX IF NOT EXISTS "IX_FlightDefinitions_DefaultAircraftId" ON "FlightDefinitions"("DefaultAircraftId");
CREATE INDEX IF NOT EXISTS "IX_FlightDefinitions_FlightNumber" ON "FlightDefinitions"("FlightNumber");

COMMENT ON TABLE "FlightScheduleTemplates" IS 'Mẫu lịch bay - chứa nhiều chuyến bay định kỳ';
COMMENT ON TABLE "FlightTemplateDetails" IS 'Chi tiết các chuyến bay trong mẫu lịch';
COMMENT ON TABLE "FlightDefinitions" IS 'Định nghĩa chuyến bay - thông tin cố định của mỗi số hiệu chuyến bay';
