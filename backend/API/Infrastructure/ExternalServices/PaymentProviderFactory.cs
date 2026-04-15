namespace API.Infrastructure.ExternalServices;

using API.Application.Interfaces;
using Microsoft.Extensions.Logging;

public interface IPaymentProviderFactory
{
    IPaymentProvider CreateProvider(string provider);
}

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentProviderFactory> _logger;

    public PaymentProviderFactory(IServiceProvider serviceProvider, ILogger<PaymentProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IPaymentProvider CreateProvider(string provider)
    {
        var normalizedProvider = provider.ToUpperInvariant();

        var providerInstance = normalizedProvider switch
        {
            "MOMO" => _serviceProvider.GetService(typeof(MomoPaymentProvider)) as IPaymentProvider,
            "VNPAY" => _serviceProvider.GetService(typeof(VnpayPaymentProvider)) as IPaymentProvider,
            "STRIPE" => _serviceProvider.GetService(typeof(StripePaymentProvider)) as IPaymentProvider,
            "PAYPAL" => _serviceProvider.GetService(typeof(PaypalPaymentProvider)) as IPaymentProvider,
            "CARD" => _serviceProvider.GetService(typeof(CardPaymentProvider)) as IPaymentProvider,
            "BANK" => _serviceProvider.GetService(typeof(BankTransferProvider)) as IPaymentProvider,
            _ => throw new NotSupportedException($"Payment provider '{provider}' is not supported")
        };

        if (providerInstance == null)
        {
            throw new InvalidOperationException($"Failed to create payment provider '{provider}'");
        }

        _logger.LogInformation("Created payment provider: {Provider}", provider);
        return providerInstance;
    }
}
