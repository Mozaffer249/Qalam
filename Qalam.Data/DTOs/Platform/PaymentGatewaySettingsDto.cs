using System.Text.Json;
using Qalam.Data.DTOs.Payment;

namespace Qalam.Data.DTOs.Platform;

/// <summary>
/// Stored as JSON in common.SystemSettings (Key = Payments.Gateway).
/// Selects which registered IPaymentGateway creates new payment intents,
/// and Moyasar presentation mode (WebView vs native SDK).
/// API keys stay in env; this row only holds selection fields.
/// </summary>
public class PaymentGatewaySettingsDto
{
    /// <summary>Active provider key (Mock, Moyasar, PayTabs, HyperPay, Stripe).</summary>
    public string ActiveProvider { get; set; } = "Mock";

    /// <summary>
    /// Moyasar checkout presentation: HostedRedirect (WebView) or NativeSdk (Flutter widgets).
    /// Ignored when ActiveProvider is not Moyasar. Falls back to env PaymentSettings:Moyasar:ClientMode.
    /// </summary>
    public string MoyasarClientMode { get; set; } = "HostedRedirect";
}

/// <summary>Admin GET response: active selection plus registry status (no secrets).</summary>
public class PaymentGatewayAdminDto
{
    public string ActiveProvider { get; set; } = "Mock";

    public string MoyasarClientMode { get; set; } = "HostedRedirect";

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
    public static PaymentGatewaySettingsDto Create(
        string? envProvider = null,
        string? envMoyasarClientMode = null) => new()
    {
        ActiveProvider = string.IsNullOrWhiteSpace(envProvider) ? "Mock" : envProvider.Trim(),
        MoyasarClientMode = NormalizeMoyasarClientMode(envMoyasarClientMode)
    };

    public static string NormalizeMoyasarClientMode(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw)
            && raw.Trim().Equals(nameof(PaymentClientMode.NativeSdk), StringComparison.OrdinalIgnoreCase))
            return nameof(PaymentClientMode.NativeSdk);

        return nameof(PaymentClientMode.HostedRedirect);
    }

    public static PaymentClientMode ToClientMode(string? raw) =>
        NormalizeMoyasarClientMode(raw) == nameof(PaymentClientMode.NativeSdk)
            ? PaymentClientMode.NativeSdk
            : PaymentClientMode.HostedRedirect;

    public static string ToJson(PaymentGatewaySettingsDto settings) =>
        JsonSerializer.Serialize(settings, JsonOptions);

    public static PaymentGatewaySettingsDto FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<PaymentGatewaySettingsDto>(json, JsonOptions) ?? Create();
        dto.MoyasarClientMode = NormalizeMoyasarClientMode(dto.MoyasarClientMode);
        if (string.IsNullOrWhiteSpace(dto.ActiveProvider))
            dto.ActiveProvider = "Mock";
        return dto;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
