using FlightBooking.Application.Services.Implementations;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Interfaces.Repositories;
using FlightBooking.Domain.Interfaces.Services;
using FlightBooking.Infrastructure.ExternalServices;
using FlightBooking.Infrastructure.ExternalServices.Email;
using FlightBooking.Infrastructure.Persistence;
using FlightBooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlightBooking.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(3)));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IOtpTokenRepository, OtpTokenRepository>();
        services.AddScoped<IFlightRepository, FlightRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IAircraftRepository, AircraftRepository>();
        services.AddScoped<IFlightInventoryRepository, FlightInventoryRepository>();
        services.AddScoped<IFlightFarePriceRepository, FlightFarePriceRepository>();
        services.AddScoped<IPriceRuleRepository, PriceRuleRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IWalletLedgerRepository, WalletLedgerRepository>();
        services.AddScoped<IIdempotencyKeyRepository, IdempotencyKeyRepository>();
        services.AddScoped<ISeatTemplateRepository, SeatTemplateRepository>();
        services.AddScoped<INotificationJobRepository, NotificationJobRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Infrastructure services
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IVerificationService, VerificationService>();

        // Flight
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IAircraftService, AircraftService>();

        // Inventory
        services.AddScoped<ISeatTemplateService, SeatTemplateService>();
        services.AddScoped<IFlightInventoryService, FlightInventoryService>();
        services.AddScoped<IInventoryReservationService, InventoryReservationService>();

        // Search / Pricing
        services.AddScoped<ISearchFlightService, SearchFlightService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IPromotionService, PromotionService>();

        // Booking / Order
        services.AddScoped<IBookingValidationService, BookingValidationService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<ITicketingService, TicketingService>();

        // Payment / Refund
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
        services.AddScoped<IPaymentReconciliationService, PaymentReconciliationService>();
        services.AddScoped<IRefundPolicyService, RefundPolicyService>();
        services.AddScoped<IRefundService, RefundService>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        // Notifications / System
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISchedulerService, SchedulerService>();

        // Admin facades
        services.AddScoped<IAdminFlightService, AdminFlightService>();
        services.AddScoped<IAdminPricingService, AdminPricingService>();
        services.AddScoped<IAdminRefundService, AdminRefundService>();

        return services;
    }
}
