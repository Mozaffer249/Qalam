using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Auth;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AuthSettingsProvider : IAuthSettingsProvider
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthSettingsProvider> _logger;

    public AuthSettingsProvider(
        ISystemSettingRepository systemSettingRepository,
        IConfiguration configuration,
        ILogger<AuthSettingsProvider> logger)
    {
        _systemSettingRepository = systemSettingRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var row = await _systemSettingRepository.GetByKeyAsync(AuthSettingsKeys.SettingsKey, cancellationToken);
        if (row != null)
        {
            try
            {
                return AuthSettingsDefaults.FromJson(row.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Auth.Settings JSON in database; using defaults");
                return AuthSettingsDefaults.Create();
            }
        }

        return LoadFromConfiguration() ?? AuthSettingsDefaults.Create();
    }

    public async Task<AuthSettingsDto> SaveSettingsAsync(
        AuthSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        await _systemSettingRepository.UpsertAsync(new SystemSetting
        {
            Key = AuthSettingsKeys.SettingsKey,
            Value = AuthSettingsDefaults.ToJson(settings),
            Type = SettingType.JSON,
            IsPublic = true,
            DescriptionEn = "Teacher and student login/register behaviour",
            DescriptionAr = "إعدادات تسجيل الدخول والتسجيل"
        }, cancellationToken);

        return settings;
    }

    public AuthConfigResponseDto ToPublicConfig(AuthSettingsDto settings) =>
        AuthConfigMapper.ToPublicConfig(settings);

    private AuthSettingsDto? LoadFromConfiguration()
    {
        var section = _configuration.GetSection("Auth:Settings");
        if (!section.Exists()) return null;
        return section.Get<AuthSettingsDto>();
    }
}

internal static class AuthConfigMapper
{
    public static AuthConfigResponseDto ToPublicConfig(AuthSettingsDto settings)
    {
        var length = Math.Max(1, settings.Otp.Length);
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var isNonProd = string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(envName, "Staging", StringComparison.OrdinalIgnoreCase);
        var allowTestCode = settings.Otp.AllowTestCodeInDevelopment && isNonProd;

        return new AuthConfigResponseDto
        {
            Teacher = MapPersona(settings.Teacher, settings.Teacher.OtpDelivery, length),
            Student = MapPersona(settings.Student, settings.Student.OtpDelivery, length),
            Otp = new AuthOtpConfigDto
            {
                Length = length,
                ExpirySeconds = settings.Otp.ExpirySeconds,
                ResendCooldownSeconds = settings.Otp.ResendCooldownSeconds,
                AllowTestCode = allowTestCode
            }
        };
    }

    private static AuthPersonaConfigDto MapPersona(PersonaAuthSettingsDto p, string delivery, int otpLength)
    {
        var isEmail = string.Equals(delivery, "Email", StringComparison.OrdinalIgnoreCase);
        return new AuthPersonaConfigDto
        {
            LoginMethod = p.LoginMethod,
            OtpDelivery = p.OtpDelivery,
            ShowPhoneField = p.RegisterRequiresPhone,
            ShowEmailField = p.RegisterRequiresEmail || isEmail,
            PhoneRequired = p.RegisterRequiresPhone,
            EmailRequired = p.RegisterRequiresEmail,
            OtpHintEn = isEmail
                ? $"We sent a {otpLength}-digit code to your email"
                : $"We sent a {otpLength}-digit code to your phone",
            OtpHintAr = isEmail
                ? $"أرسلنا رمزاً من {otpLength} أرقام إلى بريدك الإلكتروني"
                : $"أرسلنا رمزاً من {otpLength} أرقام إلى هاتفك"
        };
    }
}
