using API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/bookings", (BookingRequest request, IBookingService bookingService) =>
{
    var validationErrors = ValidateBookingRequest(request);

    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var booking = bookingService.CreateBooking(request);
    return Results.Created($"/bookings/{booking.BookingReference}", booking);
})
.WithName("CreateBooking");

static Dictionary<string, string[]> ValidateBookingRequest(BookingRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (request.FlightId <= 0)
    {
        errors[nameof(request.FlightId)] = ["FlightId must be greater than 0."];
    }

    if (request.UserId <= 0)
    {
        errors[nameof(request.UserId)] = ["UserId must be greater than 0."];
    }

    if (request.PassengerCount <= 0)
    {
        errors[nameof(request.PassengerCount)] = ["PassengerCount must be greater than 0."];
    }

    return errors;
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
