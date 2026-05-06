-- Create FlightScheduleTemplates table
CREATE TABLE "FlightScheduleTemplates" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Description" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create FlightTemplateDetails table
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
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
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

-- Create indexes
CREATE INDEX "IX_FlightTemplateDetails_TemplateId" ON "FlightTemplateDetails"("TemplateId");
CREATE INDEX "IX_FlightTemplateDetails_RouteId" ON "FlightTemplateDetails"("RouteId");
CREATE INDEX "IX_FlightTemplateDetails_AircraftId" ON "FlightTemplateDetails"("AircraftId");
CREATE INDEX "IX_FlightTemplateDetails_DayOfWeek" ON "FlightTemplateDetails"("DayOfWeek");
CREATE INDEX "IX_FlightScheduleTemplates_IsActive" ON "FlightScheduleTemplates"("IsActive");
