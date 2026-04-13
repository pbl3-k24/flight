using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Tests;

public class DbModelRegressionTests
{
    [Fact]
    public void CreateScript_UsesPostgresUtcDefault()
    {
        using var context = CreateContext();

        var script = context.Database.GenerateCreateScript();
        Assert.Contains("TIMEZONE('UTC', NOW())", script);
    }

    [Fact]
    public void CreateScript_HasCriticalFlightCheckConstraints()
    {
        using var context = CreateContext();
        var script = context.Database.GenerateCreateScript();

        Assert.Contains("CK_Flights_TotalSeats_Positive", script);
        Assert.Contains("CK_Flights_AvailableSeats_Range", script);
        Assert.Contains("CK_Flights_Time_Order", script);
        Assert.Contains("CK_Flights_Route_DifferentAirports", script);
        Assert.Contains("CK_Flights_BasePrice_NonNegative", script);
    }

    [Fact]
    public void CreateScript_HasPhase1AuthAccountTablesAndConstraints()
    {
        using var context = CreateContext();
        var script = context.Database.GenerateCreateScript();

        Assert.Contains("\"Roles\"", script);
        Assert.Contains("\"UserRoles\"", script);
        Assert.Contains("\"EmailVerificationTokens\"", script);
        Assert.Contains("\"PasswordResetTokens\"", script);
        Assert.Contains("IX_Roles_Name", script);
        Assert.Contains("IX_Users_GoogleId", script);
        Assert.Contains("CK_Users_FullName_NotEmpty", script);
    }

    private static FlightBookingDbContext CreateContext()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("FLIGHT_TEST_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=flight_test;Username=test_user_placeholder;Password=test_password_placeholder";

        var options = new DbContextOptionsBuilder<FlightBookingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FlightBookingDbContext(options);
    }
}
