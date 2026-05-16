using API.Application.Interfaces;
using API.Application.Services;
using API.Extensions;
using API.Infrastructure.Data;
using API.Infrastructure.ExternalServices;
using API.Infrastructure.Repositories;
using API.Infrastructure.Security;
using API.Infrastructure.Services;
using API.Infrastructure.UnitOfWork;
using API.Middleware;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

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
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddScoped<IDataProtectionService, DataProtectionService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<AuditService>();

// Register application services - Phase 1: Authentication
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ICsvSeedService, CsvSeedService>();

// Register application services - Phase 2: Flight Search & Booking
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAdditionalServiceService, AdditionalServiceService>();
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
builder.Services.AddScoped<IAircraftAdminService, AircraftAdminService>();
builder.Services.AddScoped<IFlightTemplateService, FlightTemplateService>();
builder.Services.AddScoped<IFlightDefinitionService, FlightDefinitionService>();

// Register application services - Phase 5: Notifications, Logging & Dashboard
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

// Register application services - Phase 6: Advanced Analytics, Reporting & Realtime
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
builder.Services.AddScoped<IRealtimeDashboardService, RealtimeDashboardService>();
builder.Services.AddScoped<IPerformanceAnalyticsService, PerformanceAnalyticsService>();

// Register payment providers
builder.Services.AddScoped<VnpayPaymentProvider>();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

// Repository registrations
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IFlightSeatInventoryRepository, FlightSeatInventoryRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IRefundRequestRepository, RefundRequestRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IAircraftRepository, AircraftRepository>();
builder.Services.AddScoped<ISeatClassRepository, SeatClassRepository>();
builder.Services.AddScoped<IBookingPassengerRepository, BookingPassengerRepository>();

// Unit of Work for atomic transaction operations
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

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

// Background services
builder.Services.AddScoped<API.Application.Interfaces.IBackgroundJobService, API.Application.Services.BackgroundJobService>();
builder.Services.AddHostedService<API.Application.Services.BookingExpirationHostedService>();

// Register VNPAY Refund Worker
builder.Services.AddHostedService<API.Application.Services.VnpayRefundHostedService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0)
        {
            // Safe fallback for local demo only when not configured.
            allowedOrigins = ["http://localhost:3000", "http://localhost:5173"];
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

var databaseStartupEnabled = builder.Configuration.GetValue("DatabaseStartup:Enabled", false);
if (databaseStartupEnabled)
{
    using var scope = app.Services.CreateScope();
    var scopedProvider = scope.ServiceProvider;
    var logger = scopedProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseInitialization");

    try
    {
        var dbContext = scopedProvider.GetRequiredService<FlightBookingDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (!canConnect)
        {
            logger.LogWarning("Cannot connect to database. Please ensure the database exists and connection is valid.");
        }
        else
        {
            logger.LogInformation("✓ Database connection established");

            var schemaSyncEnabled = builder.Configuration.GetValue("DatabaseStartup:SchemaSync", false);
            if (schemaSyncEnabled)
            {
                await DatabaseSchemaSync.SyncSchemaAsync(dbContext, logger);
            }

            var initializerEnabled = builder.Configuration.GetValue("DatabaseStartup:InitialSeed", false);
            if (initializerEnabled)
            {
                await DbInitializer.InitializeAsync(dbContext, logger);
            }

            await app.SeedCsvDataAsync();
        }

        logger.LogInformation("✓ Database startup tasks completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database");

        if (!app.Environment.IsDevelopment())
        {
            logger.LogCritical("Database startup tasks failed in PRODUCTION environment. Application startup aborted.");
            throw;
        }

        logger.LogWarning("Running in DEVELOPMENT mode without database startup tasks.");
    }
}
else
{
    app.Logger.LogInformation("Database startup tasks are disabled. DB schema, initialization, and CSV seed are controlled externally.");
}

// Generate search test data
// TODO: SampleDataForSearching needs refactoring for FlightDefinition
/*
using (var scope = app.Services.CreateScope())
{
    var scopedProvider = scope.ServiceProvider;
    var logger = scopedProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("DataSeeding");
    try
    {
        var dbContext = scopedProvider.GetRequiredService<FlightBookingDbContext>();
        logger.LogInformation("Starting sample search data seeding...");
        await SampleDataForSearching.AddSearchTestDataAsync(dbContext);
        logger.LogInformation("Sample search data seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed sample search data");
    }
}
*/

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

app.UseCors("FrontendOnly");

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


