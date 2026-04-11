using FlightBooking.Application.DTOs.Booking;
using FlightBooking.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(IBookingService bookingService, ICheckoutService checkoutService, ITicketingService ticketingService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var booking = await bookingService.GetByIdAsync(id);
        return Ok(booking);
    }

    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var booking = await bookingService.GetByCodeAsync(code);
        return Ok(booking);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bookings = await bookingService.GetByUserAsync(userId, page, pageSize);
        return Ok(bookings);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var session = await checkoutService.InitiateCheckoutAsync(request, userId);
        return Ok(session);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await bookingService.CancelAsync(id, reason, userId);
        return NoContent();
    }

    [HttpGet("{bookingId}/tickets")]
    public async Task<IActionResult> GetTickets(Guid bookingId)
    {
        var tickets = await ticketingService.GetByBookingAsync(bookingId);
        return Ok(tickets);
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var bookings = await bookingService.GetAllAsync(page, pageSize);
        return Ok(bookings);
    }
}
