namespace API.Application.Services;

using API.Application.Dtos.AdditionalService;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class AdditionalServiceService : IAdditionalServiceService
{
    private readonly FlightBookingDbContext _context;

    public AdditionalServiceService(FlightBookingDbContext context)
    {
        _context = context;
    }

    public async Task<List<AdditionalServiceDto>> GetAllServicesAsync()
    {
        var services = await _context.AdditionalServices
            .Where(s => !s.IsDeleted)
            .Select(s => new AdditionalServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Price = s.Price,
                Description = s.Description
            })
            .ToListAsync();

        return services;
    }

    public async Task<SeatClassServiceConfigDto> GetServicesBySeatClassAsync(int seatClassId)
    {
        var includedServiceIds = await _context.ClassServiceConfigs
            .Where(c => c.SeatClassId == seatClassId && c.IsIncluded)
            .Select(c => c.AdditionalServiceId)
            .ToListAsync();

        var allServices = await _context.AdditionalServices
            .Where(s => !s.IsDeleted)
            .ToListAsync();

        var includedServices = allServices
            .Where(s => includedServiceIds.Contains(s.Id))
            .Select(s => new AdditionalServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Price = 0, // Included is free
                Description = s.Description
            }).ToList();

        var optionalServices = allServices
            .Where(s => !includedServiceIds.Contains(s.Id))
            .Select(s => new AdditionalServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Price = s.Price,
                Description = s.Description
            }).ToList();

        return new SeatClassServiceConfigDto
        {
            SeatClassId = seatClassId,
            IncludedServices = includedServices,
            OptionalServices = optionalServices
        };
    }
}
