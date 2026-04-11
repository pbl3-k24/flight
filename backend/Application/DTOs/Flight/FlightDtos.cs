namespace FlightBooking.Application.DTOs.Flight;

// Airport
public record AirportDto(Guid Id, string Code, string Name, string City, string Country, bool IsActive);

// Route
public record RouteDto(Guid Id, string OriginCode, string OriginCity, string DestinationCode, string DestinationCity, bool IsDomestic, bool IsActive);
public record CreateRouteRequest(Guid OriginAirportId, Guid DestinationAirportId, int? DistanceKm);
public record UpdateRouteRequest(int? DistanceKm, bool? IsActive);

// Aircraft
public record AircraftDto(Guid Id, string Code, string Model, string? Manufacturer, int TotalSeats, bool IsActive);
public record CreateAircraftRequest(string Code, string Model, string? Manufacturer, int TotalSeats);
public record UpdateAircraftRequest(string? Model, string? Manufacturer, int? TotalSeats, bool? IsActive);

// Flight
public record FlightDto(
    Guid Id,
    string FlightNumber,
    RouteDto Route,
    AircraftDto Aircraft,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    string Status,
    string? GateNumber,
    IEnumerable<FlightInventoryDto> Inventories,
    IEnumerable<FarePriceDto> FarePrices);

public record CreateFlightRequest(
    string FlightNumber,
    Guid RouteId,
    Guid AircraftId,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    string? GateNumber);

public record UpdateFlightRequest(
    DateTime? DepartureTime,
    DateTime? ArrivalTime,
    string? GateNumber,
    string? Status,
    string? DelayReason);

// Fare Class
public record FareClassDto(Guid Id, string Code, string Name, int CheckedBaggageKg, int CabinBaggageKg, bool IsRefundable, bool IsChangeable);

// Flight Inventory
public record FlightInventoryDto(Guid FareClassId, string FareClassCode, int TotalSeats, int AvailableSeats, int HeldSeats);

// Fare Price
public record FarePriceDto(Guid FareClassId, string FareClassCode, decimal CurrentPrice, string PriceSource);

// Seat Template
public record SeatTemplateDto(Guid Id, string SeatNumber, string FareClassCode, string? SeatType);
public record CreateSeatTemplateRequest(string SeatNumber, string FareClassCode, string? SeatType);

// Search
public record FlightSearchRequest(
    string OriginCode,
    string DestinationCode,
    DateOnly DepartureDate,
    int AdultCount,
    int ChildCount = 0,
    int InfantCount = 0,
    string? FareClassCode = null);

public record RoundTripSearchRequest(
    string OriginCode,
    string DestinationCode,
    DateOnly DepartureDate,
    DateOnly ReturnDate,
    int AdultCount,
    int ChildCount = 0,
    int InfantCount = 0);

public record FlightSearchResultDto(
    FlightDto Flight,
    IEnumerable<FareOptionDto> FareOptions);

public record FareOptionDto(
    Guid FareClassId,
    string FareClassCode,
    string FareClassName,
    int AvailableSeats,
    decimal PricePerPerson,
    decimal TotalPrice,
    bool IsRefundable,
    int CheckedBaggageKg);

// Pricing
public record PriceBreakdownDto(
    Guid FlightId,
    Guid FareClassId,
    decimal BaseFare,
    decimal Tax,
    decimal ServiceFee,
    decimal TotalPerPerson,
    string PriceSource);

// Price Rules
public record PriceRuleDto(Guid Id, Guid? RouteId, Guid? FareClassId, string? Season, decimal BasePrice, decimal Multiplier, bool IsActive);
public record CreatePriceRuleRequest(Guid? RouteId, Guid? FareClassId, string? Season, string? DayOfWeek, int? DaysBeforeDeparture, decimal BasePrice, decimal Multiplier);
public record UpdatePriceRuleRequest(decimal? BasePrice, decimal? Multiplier, bool? IsActive);

// Promotion
public record PromotionDto(Guid Id, string Code, string Name, string DiscountType, decimal DiscountValue, bool IsActive, DateTime StartDate, DateTime EndDate);
public record CreatePromotionRequest(string Code, string Name, string DiscountType, decimal DiscountValue, decimal? MinOrderAmount, decimal? MaxDiscountAmount, int? UsageLimit, DateTime StartDate, DateTime EndDate);
public record UpdatePromotionRequest(string? Name, decimal? DiscountValue, bool? IsActive, DateTime? EndDate);

// Price Override Log
public record PriceOverrideLogDto(Guid Id, Guid FlightFarePriceId, Guid AdminId, decimal PriceBefore, decimal PriceAfter, string? Reason, DateTime CreatedAt);
