using System.Text.Json;

namespace Qalam.Data.DTOs.Platform;

/// <summary>
/// Stored as JSON in common.SystemSettings (Key = Payments.Gateway).
/// Selects which registered IPaymentGateway creates new payment intents.
/// API keys stay in env; this row only holds the active provider name.
/// </summary>
public class PaymentGatewaySettingsDto
{
    /// <summary>Active provider key (Mock, Moyasar, PayTabs, HyperPay, Stripe).</summary>
    public string ActiveProvider { get; set; } = "Mock";
}

/// <summary>Admin GET response: active selection plus registry status (no secrets).</summary>
public class PaymentGatewayAdminDto
{
    public string ActiveProvider { get; set; } = "Mock";

    public List<PaymentGatewayInfoDto> Providers { get; set; } = new();
}

public class PaymentGatewayInfoDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public string ClientMode { get; set; } = string.Empty;
    public bool SupportsRefund { get; set; } = true;
}

public static class PaymentGatewaySettingsDefaults
{
    public static PaymentGatewaySettingsDto Create(string? envProvider = null) => new()
    {
        ActiveProvider = string.IsNullOrWhiteSpace(envProvider) ? "Mock" : envProvider.Trim()
    };

    public static string ToJson(PaymentGatewaySettingsDto settings) =>
        JsonSerializer.Serialize(settings, JsonOptions);

    public static PaymentGatewaySettingsDto FromJson(string json) =>
        JsonSerializer.Deserialize<PaymentGatewaySettingsDto>(json, JsonOptions) ?? Create();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
