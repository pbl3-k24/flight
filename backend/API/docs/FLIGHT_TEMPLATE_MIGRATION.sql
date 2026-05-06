-- Migration: Add Flight Schedule Templates
-- Purpose: Create tables for storing reusable flight schedule templates

-- Table 1: FlightScheduleTemplates (Template container)
CREATE TABLE "FlightScheduleTemplates" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Description" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Table 2: FlightTemplateDetails (Individual flight definitions)
CREATE TABLE "FlightTemplateDetails" (
    "Id" SERIAL PRIMARY KEY,
    "TemplateId" INTEGER NOT NULL,
    "RouteId" INTEGER NOT NULL,
    "AircraftId" INTEGER NOT NULL,
    "DayOfWeek" INTEGER NOT NULL CHECK ("DayOfWeek" >= 0 AND "DayOfWeek" <= 6),
    "DepartureTime" TIME NOT NULL,
    "ArrivalTime" TIME NOT NULL,
    "FlightNumberPrefix" VARCHAR(10) NOT NULL,
    "FlightNumberSuffix" VARCHAR(10) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign Keys
    CONSTRAINT "FK_FlightTemplateDetails_Templates" 
        FOREIGN KEY ("TemplateId") 
        REFERENCES "FlightScheduleTemplates"("Id") 
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_FlightTemplateDetails_Routes" 
        FOREIGN KEY ("RouteId") 
        REFERENCES "Routes"("Id") 
        ON DELETE RESTRICT,
    
    CONSTRAINT "FK_FlightTemplateDetails_Aircraft" 
        FOREIGN KEY ("AircraftId") 
        REFERENCES "Aircraft"("Id") 
        ON DELETE RESTRICT
);

-- Indexes for performance
CREATE INDEX "IX_FlightTemplateDetails_TemplateId" ON "FlightTemplateDetails"("TemplateId");
CREATE INDEX "IX_FlightTemplateDetails_RouteId" ON "FlightTemplateDetails"("RouteId");
CREATE INDEX "IX_FlightTemplateDetails_AircraftId" ON "FlightTemplateDetails"("AircraftId");
CREATE INDEX "IX_FlightTemplateDetails_DayOfWeek" ON "FlightTemplateDetails"("DayOfWeek");
CREATE INDEX "IX_FlightScheduleTemplates_IsActive" ON "FlightScheduleTemplates"("IsActive");

-- Sample data (optional)
INSERT INTO "FlightScheduleTemplates" ("Name", "Description", "IsActive", "CreatedAt", "UpdatedAt")
VALUES 
    ('Lịch bay mùa hè 2026', 'Lịch bay HAN-SGN mùa hè', TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Note: You need to replace RouteId and AircraftId with actual IDs from your database
-- INSERT INTO "FlightTemplateDetails" ("TemplateId", "RouteId", "AircraftId", "DayOfWeek", "DepartureTime", "ArrivalTime", "FlightNumberPrefix", "FlightNumberSuffix", "CreatedAt")
-- VALUES 
--     (1, 1, 1, 1, '08:00:00', '10:15:00', 'VN', '201', CURRENT_TIMESTAMP),
--     (1, 1, 2, 1, '14:00:00', '16:15:00', 'VN', '202', CURRENT_TIMESTAMP);
