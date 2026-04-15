using API.Application.Interfaces;
using API.Application.Services;
using API.Infrastructure.Data;
using API.Infrastructure.ExternalServices;
using API.Infrastructure.Repositories;
using API.Infrastructure.Security;
using API.Infrastructure.Services;
using API.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient(); // Add HttpClient for payment providers

// Register Phase 7: Security & Validation
builder.Services.AddDataProtection();
builder.Services.AddScoped<IDataProtectionService, DataProtectionService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<AuditService>();

// Register application services - Phase 1: Authentication
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Register application services - Phase 2: Flight Search & Booking
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

// Register application services - Phase 3: Payment & Ticketing
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IRefundService, RefundService>();

// Register application services - Phase 4: Admin Management
builder.Services.AddScoped<IFlightAdminService, FlightAdminService>();
builder.Services.AddScoped<IBookingAdminService, BookingAdminService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IPromotionAdminService, PromotionAdminService>();

// Register application services - Phase 5: Notifications, Logging & Dashboard
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

// Register application services - Phase 6: Advanced Analytics, Reporting & Realtime
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
builder.Services.AddScoped<IRealtimeDashboardService, RealtimeDashboardService>();
builder.Services.AddScoped<IPerformanceAnalyticsService, PerformanceAnalyticsService>();

// Register payment providers
builder.Services.AddScoped<MomoPaymentProvider>();
builder.Services.AddScoped<VnpayPaymentProvider>();
builder.Services.AddScoped<StripePaymentProvider>();
builder.Services.AddScoped<PaypalPaymentProvider>();
builder.Services.AddScoped<CardPaymentProvider>();
builder.Services.AddScoped<BankTransferProvider>();
builder.Services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();

// Register repositories - All Phases
// NOTE: IMPORTANT - Repository implementations are REQUIRED for functionality
// Phase 1 repositories (already implemented)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

// Phase 2-6 repositories (temporary placeholders - need real implementation)
// TODO: Implement all repositories with actual database logic
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable reference type
builder.Services.AddScoped<IFlightRepository>(sp => null!);
builder.Services.AddScoped<IBookingRepository>(sp => null!);
builder.Services.AddScoped<IFlightSeatInventoryRepository>(sp => null!);
builder.Services.AddScoped<IPromotionRepository>(sp => null!);
builder.Services.AddScoped<IPaymentRepository>(sp => null!);
builder.Services.AddScoped<ITicketRepository>(sp => null!);
builder.Services.AddScoped<IRefundRequestRepository>(sp => null!);
builder.Services.AddScoped<IAuditLogRepository>(sp => null!);
builder.Services.AddScoped<INotificationLogRepository>(sp => null!);
builder.Services.AddScoped<IRoleRepository>(sp => null!);
builder.Services.AddScoped<IAirportRepository>(sp => null!);
builder.Services.AddScoped<IRouteRepository>(sp => null!);
builder.Services.AddScoped<IAircraftRepository>(sp => null!);
builder.Services.AddScoped<ISeatClassRepository>(sp => null!);
builder.Services.AddScoped<IBookingPassengerRepository>(sp => null!);
#pragma warning restore CS8600

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";

builder.Services.AddDbContext<FlightBookingDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "flight-booking:";
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply database migrations (optional - can be skipped if database is not available)
using (var scope = app.Services.CreateScope())
{
    var scopedProvider = scope.ServiceProvider;
    var logger = scopedProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseMigration");

    try
    {
        var dbContext = scopedProvider.GetRequiredService<FlightBookingDbContext>();
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
        
        if (pendingMigrations.Count > 0)
        {
            logger.LogInformation("Found {Count} pending migrations. Applying migrations...", pendingMigrations.Count);
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("No pending migrations. Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations. Continuing without database...");
        logger.LogWarning("The application will continue to run, but database operations will fail.");
        // Don't throw - allow app to start even without database
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Flight Ticket Booking API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

// Phase 7: Security & Validation Middleware
app.UseGlobalExceptionHandling();
app.UseSecurityHeaders();
app.UseRateLimiting();
app.UseRequestLogging();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
