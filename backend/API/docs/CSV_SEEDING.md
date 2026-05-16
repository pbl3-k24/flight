# CSV Seeding

CSV seed files live in `API/seed-data`. The seeder runs at startup when `CsvSeed:Enabled` is `true`.

The seeder is idempotent: it maps foreign keys by code, skips rows whose business code already exists, and never hard-codes IDs.

## File Order

1. `airports.csv`
2. `aircrafts.csv`
3. `seat_classes.csv`
4. `aircraft_seat_templates.csv`
5. `routes.csv`
6. `flight_definitions.csv`
7. `weekly_flight_templates.csv`

## Schemas

### airports.csv

```csv
Code,Name,City,Province,IsActive
SGN,Tan Son Nhat International Airport,Ho Chi Minh City,Ho Chi Minh,true
```

`Code` maps to `Airport.Code`.

### aircrafts.csv

```csv
AircraftCode,Model,TotalSeats,IsActive
VN-A320-001,Airbus A320,180,true
```

`AircraftCode` maps to `Aircraft.RegistrationNumber`.

### seat_classes.csv

```csv
Code,Name,RefundPercent,ChangeFee,Priority
ECO,Economy,50,300000,3
```

`Code` maps to `SeatClass.Code`.

### aircraft_seat_templates.csv

```csv
AircraftCode,SeatClassCode,DefaultSeatCount,DefaultBasePrice
VN-A320-001,ECO,150,1200000
```

`AircraftCode` maps to `Aircraft.Id`; `SeatClassCode` maps to `SeatClass.Id`.

Business rule: for each aircraft, sum of `DefaultSeatCount` must equal `Aircraft.TotalSeats`.

### routes.csv

```csv
RouteCode,DepartureAirportCode,ArrivalAirportCode,DistanceKm,EstimatedDurationMinutes,IsActive
SGN-HAN,SGN,HAN,1166,145,true
```

`RouteCode` maps to `Route.Code`. Departure and arrival airport codes must be different.

### flight_definitions.csv

```csv
FlightDefinitionCode,RouteCode,AircraftCode,DepartureTime,ArrivalTime,ArrivalOffsetDays,IsActive
VN201,SGN-HAN,VN-A320-001,06:00,08:25,0,true
```

`FlightDefinitionCode` maps to `FlightDefinition.FlightNumber`.

Time format is `HH:mm`. If `ArrivalTime` is earlier than `DepartureTime`, `ArrivalOffsetDays` must be greater than `0`; blank offset is auto-resolved to `1` for overnight flights and `0` otherwise.

### weekly_flight_templates.csv

```csv
TemplateCode,TemplateName,Description,EffectiveFrom,EffectiveTo,FlightDefinitionCode,DayOfWeek,AircraftOverrideCode,DepartureTimeOverride,ArrivalTimeOverride,ArrivalOffsetDaysOverride,IsActive
SUMMER_2026,Summer 2026,Default summer schedule,2026-06-01,2026-08-31,VN201,1,,,,,true
```

`DayOfWeek`: `0=Sunday`, `1=Monday`, ..., `6=Saturday`.

Override columns are optional. If omitted, the system uses the values from `FlightDefinition`.

## Run

Set config:

```json
"CsvSeed": {
  "Enabled": true,
  "Directory": "seed-data"
}
```

Start the API. The seeder logs inserted/skipped counts per file. If CSV data is invalid, startup logs all validation errors and rolls back the CSV seed transaction.

## Edge Cases

- Missing required column or empty required value.
- FK code not found.
- Duplicate business code.
- Route departure equals arrival.
- Aircraft seat template total does not match `TotalSeats`.
- Invalid `HH:mm` time format.
- Invalid overnight flight offset.
- Duplicate weekly detail for the same template, flight definition, day, and departure time.
