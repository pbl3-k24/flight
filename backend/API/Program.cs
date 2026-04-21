using API.Application.Interfaces;
using API.Application.Services;
using API.Infrastructure.Data;
using API.Infrastructure.ExternalServices;
using API.Infrastructure.Repositories;
using API.Infrastructure.Security;
using API.Infrastructure.Services;
using API.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen(options =>
{
    // JWT Bearer token configuration
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT Bearer token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Add operation filter to apply Bearer token to all endpoints
    options.OperationFilter<API.Infrastructure.Swagger.AuthorizeOperationFilter>();
});
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
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IBookingRepository>(sp => null!);
builder.Services.AddScoped<IFlightSeatInventoryRepository, FlightSeatInventoryRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPaymentRepository>(sp => null!);
builder.Services.AddScoped<ITicketRepository>(sp => null!);
builder.Services.AddScoped<IRefundRequestRepository>(sp => null!);
builder.Services.AddScoped<IAuditLogRepository>(sp => null!);
builder.Services.AddScoped<INotificationLogRepository>(sp => null!);
builder.Services.AddScoped<IRoleRepository>(sp => null!);
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IAircraftRepository, AircraftRepository>();
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

// Configure JWT Bearer authentication using Microsoft.AspNetCore.Authentication.JwtBearer
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>("Bearer", null);

// Configure Authorization policies
builder.Services.AddAuthorization();

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

        // Seed sample data nếu database là trống
        logger.LogInformation("Seeding sample data...");
        await DbInitializer.InitializeDatabaseAsync(dbContext);
        logger.LogInformation("Sample data seeding completed.");

        // Add additional search test data in development mode
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Adding comprehensive search test data...");
            await SampleDataForSearching.AddSearchTestDataAsync(dbContext);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");

        // In PRODUCTION: Fail hard - don't allow app to run without database
        if (!app.Environment.IsDevelopment())
        {
            logger.LogCritical("Database initialization failed in PRODUCTION environment. " +
                "Application startup aborted to prevent serving with broken database.");
            throw;  // Re-throw to stop application startup
        }

        // In DEVELOPMENT: Allow to continue with warning
        logger.LogWarning("Running in DEVELOPMENT mode without database. " +
            "Database operations will fail.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Flight Ticket Booking API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.DefaultModelsExpandDepth(1);
        options.EnablePersistAuthorization(); // Lưu token khi đóng browser
    });
}

// Phase 7: Security & Validation Middleware
app.UseGlobalExceptionHandling();
app.UseSecurityHeaders();
app.UseRateLimiting();
app.UseRequestLogging();

app.UseCors("AllowAll");

// JWT Authentication middleware - MUST be BEFORE UseHttpsRedirection
// so Authorization header is processed before any redirects
app.UseJwtAuthenticationMiddleware();

// HTTPS Redirect only in production to avoid stripping Authorization headers in development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Authentication & Authorization middleware (MUST be in this order)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
