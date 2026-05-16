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

    public async Task<AdditionalServiceDto> CreateServiceAsync(CreateAdditionalServiceDto dto)
    {
        var service = new API.Domain.Entities.AdditionalService
        {
            ServiceName = dto.ServiceName,
            Price = dto.Price,
            Description = dto.Description
        };

        _context.AdditionalServices.Add(service);
        await _context.SaveChangesAsync();

        return new AdditionalServiceDto
        {
            Id = service.Id,
            ServiceName = service.ServiceName,
            Price = service.Price,
            Description = service.Description
        };
    }

    public async Task<AdditionalServiceDto> UpdateServiceAsync(int id, UpdateAdditionalServiceDto dto)
    {
        var service = await _context.AdditionalServices
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (service == null)
        {
            throw new Application.Exceptions.NotFoundException("Additional service not found");
        }

        service.ServiceName = dto.ServiceName;
        service.Price = dto.Price;
        service.Description = dto.Description;

        _context.AdditionalServices.Update(service);
        await _context.SaveChangesAsync();

        return new AdditionalServiceDto
        {
            Id = service.Id,
            ServiceName = service.ServiceName,
            Price = service.Price,
            Description = service.Description
        };
    }

    public async Task<bool> DeleteServiceAsync(int id)
    {
        var service = await _context.AdditionalServices
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (service == null)
        {
            throw new Application.Exceptions.NotFoundException("Additional service not found");
        }

        service.SoftDelete();
        _context.AdditionalServices.Update(service);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateClassServiceConfigsAsync(int seatClassId, SeatClassServiceConfigRequestDto request)
    {
        var seatClass = await _context.SeatClasses.FindAsync(seatClassId);
        if (seatClass == null)
        {
            throw new Application.Exceptions.NotFoundException("Seat class not found");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingConfigs = await _context.ClassServiceConfigs
                .Where(c => c.SeatClassId == seatClassId)
                .ToListAsync();

            _context.ClassServiceConfigs.RemoveRange(existingConfigs);

            var newConfigs = request.IncludedServiceIds.Select(serviceId => new API.Domain.Entities.ClassServiceConfig
            {
                SeatClassId = seatClassId,
                AdditionalServiceId = serviceId,
                IsIncluded = true
            });

            _context.ClassServiceConfigs.AddRange(newConfigs);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
