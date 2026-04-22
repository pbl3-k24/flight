namespace API.Infrastructure.Repositories;

using API.Application.Interfaces;
using API.Domain.Entities;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class TicketRepository : ITicketRepository
{
    private readonly FlightBookingDbContext _context;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(FlightBookingDbContext context, ILogger<TicketRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<Ticket?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket by id: {Id}", id);
            throw;
        }
    }

    public async Task<Ticket?> GetByTicketNumberAsync(string ticketNumber)
    {
        try
        {
            return await _context.Tickets.FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket by number: {Number}", ticketNumber);
            throw;
        }
    }

    public async Task<Ticket?> GetByPassengerIdAsync(int passengerId)
    {
        try
        {
            return await _context.Tickets.FirstOrDefaultAsync(t => t.BookingPassengerId == passengerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket by passenger id: {PassengerId}", passengerId);
            throw;
        }
    }

    public async Task<IEnumerable<Ticket>> GetByBookingPassengerIdAsync(int bookingPassengerId)
    {
        try
        {
            return await _context.Tickets
                .Where(t => t.BookingPassengerId == bookingPassengerId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets for booking passenger: {BookingPassengerId}", bookingPassengerId);
            throw;
        }
    }

    public async Task<IEnumerable<Ticket>> GetAllAsync()
    {
        try
        {
            return await _context.Tickets.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tickets");
            throw;
        }
    }

    public async Task<Ticket> CreateAsync(Ticket ticket)
    {
        try
        {
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket");
            throw;
        }
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        try
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket");
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var ticket = await GetByIdAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ticket");
            throw;
        }
    }
}
