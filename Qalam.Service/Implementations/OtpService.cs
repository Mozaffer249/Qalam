using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Email;
using Qalam.Service.Models;

namespace Qalam.Service.Implementations;

public class OtpService : IOtpService
{
    public const string TestOtpCode = IOtpService.TestOtpCode;

    private readonly IPhoneOtpRepository _phoneOtpRepository;
    private readonly ILoginOtpRepository _loginOtpRepository;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IAuthLoginOtpHelper _authLoginOtpHelper;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IPhoneOtpRepository phoneOtpRepository,
        ILoginOtpRepository loginOtpRepository,
        IEmailService emailService,
        ISmsService smsService,
        IAuthLoginOtpHelper authLoginOtpHelper,
        IHostEnvironment hostEnvironment,
        ILogger<OtpService> logger)
    {
        _phoneOtpRepository = phoneOtpRepository;
        _loginOtpRepository = loginOtpRepository;
        _emailService = emailService;
        _smsService = smsService;
        _authLoginOtpHelper = authLoginOtpHelper;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<string?> GeneratePhoneOtpAsync(string countryCode, string phoneNumber)
    {
        var hasValidOtp = await _phoneOtpRepository.HasValidOtpAsync(phoneNumber);
        if (hasValidOtp)
        {
            _logger.LogWarning("Valid OTP already exists for phone {Phone}", phoneNumber);
            return null;
        }

        await _phoneOtpRepository.RemoveExpiredOtpsAsync(phoneNumber);
        await _phoneOtpRepository.SaveChangesAsync();

        var otpCode = GenerateCode(4);
        var otp = new PhoneConfirmationOtp
        {
            CountryCode = countryCode,
            PhoneNumber = phoneNumber,
            OtpCode = otpCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(30),
            IsUsed = false
        };

        await _phoneOtpRepository.AddAsync(otp);
        _logger.LogInformation("Legacy phone OTP generated for {Phone}", phoneNumber);
        return otpCode;
    }

    public async Task<bool> VerifyPhoneOtpAsync(string phoneNumber, string otpCode)
    {
        if (IsTestCodeAllowed(otpCode))
        {
            _logger.LogInformation("Test OTP used for phone {Phone}", phoneNumber);
            return true;
        }

        var otp = await _phoneOtpRepository.GetValidOtpAsync(phoneNumber, otpCode);
        if (otp == null)
        {
            _logger.LogWarning("Invalid or expired legacy OTP for phone {Phone}", phoneNumber);
            return false;
        }

        return true;
    }

    public async Task SendOtpSmsAsync(string fullPhoneNumber, string otpCode)
    {
        _logger.LogInformation("SMS OTP to {Phone}: {OTP}", fullPhoneNumber, otpCode);
        try
        {
            await _smsService.SendSmsAsync(fullPhoneNumber, $"Your Qalam verification code is: {otpCode}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMS send failed for {Phone}; OTP logged only", fullPhoneNumber);
        }
    }

    public async Task<LoginOtpSendResult?> GenerateAndSendLoginOtpAsync(
        LoginOtpSendOptions options,
        CancellationToken cancellationToken = default)
    {
        var phone = options.PhoneNumber;
        if (await _loginOtpRepository.HasValidOtpAsync(phone, cancellationToken))
        {
            _logger.LogWarning("Valid login OTP already exists for phone {Phone}", phone);
            return null;
        }

        await _loginOtpRepository.RemoveExpiredOtpsAsync(phone, cancellationToken);

        var useEmail = string.Equals(options.PersonaSettings.OtpDelivery, "Email", StringComparison.OrdinalIgnoreCase);
        var deliveryEmail = _authLoginOtpHelper.ResolveDeliveryEmail(new LoginOtpEmailContext
        {
            Settings = options.PersonaSettings,
            IsNewUser = options.IsNewUser,
            RequestEmail = options.RequestEmail,
            ExistingUserEmail = options.ExistingUserEmail
        });

        if (useEmail && string.IsNullOrEmpty(deliveryEmail))
            throw new InvalidOperationException("Email is required to send OTP.");

        var channel = useEmail ? LoginOtpChannel.Email : LoginOtpChannel.Sms;
        var fullPhone = $"{options.CountryCode}{phone}";
        var deliveryDestination = useEmail ? deliveryEmail! : fullPhone;
        var otpCode = GenerateCode(options.OtpSettings.Length);
        var expiresAt = DateTime.UtcNow.AddSeconds(options.OtpSettings.ExpirySeconds);

        var loginOtp = new LoginOtp
        {
            Channel = channel,
            PhoneNumber = phone,
            CountryCode = options.CountryCode,
            PendingEmail = options.RequestEmail?.Trim(),
            DeliveryDestination = deliveryDestination,
            OtpCode = otpCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

        await _loginOtpRepository.AddAsync(loginOtp, cancellationToken);

        if (channel == LoginOtpChannel.Email)
        {
            var expiryMinutes = Math.Max(1, options.OtpSettings.ExpirySeconds / 60);
            var subject = LoginOtpEmailTemplate.BuildSubject(options.Persona);
            var body = LoginOtpEmailTemplate.BuildHtmlBody(otpCode, expiryMinutes, options.Persona);
            await _emailService.SendEmailAsync(deliveryEmail!, subject, body);
            _logger.LogInformation("Login OTP emailed to {Email} for phone {Phone}", deliveryEmail, phone);
        }
        else
        {
            await SendOtpSmsAsync(fullPhone, otpCode);
        }

        return new LoginOtpSendResult
        {
            Channel = channel,
            OtpSentTo = channel == LoginOtpChannel.Email ? "email" : "sms",
            MaskedDestination = channel == LoginOtpChannel.Email
                ? _authLoginOtpHelper.MaskEmail(deliveryEmail!)
                : _authLoginOtpHelper.MaskPhone(phone)
        };
    }

    public async Task<bool> VerifyLoginOtpAsync(
        string phoneNumber,
        string otpCode,
        bool allowTestCode,
        CancellationToken cancellationToken = default)
    {
        if (allowTestCode && IsTestCodeAllowed(otpCode))
        {
            _logger.LogInformation("Test login OTP used for phone {Phone}", phoneNumber);
            return true;
        }

        var otp = await _loginOtpRepository.GetValidOtpAsync(phoneNumber, otpCode, cancellationToken);
        return otp != null;
    }

    public Task<LoginOtp?> GetValidLoginOtpAsync(
        string phoneNumber,
        string otpCode,
        CancellationToken cancellationToken = default) =>
        _loginOtpRepository.GetValidOtpAsync(phoneNumber, otpCode, cancellationToken);

    public Task MarkLoginOtpUsedAsync(int otpId, int? userId, CancellationToken cancellationToken = default) =>
        _loginOtpRepository.MarkAsUsedAsync(otpId, userId, cancellationToken);

    private bool IsTestCodeAllowed(string otpCode) =>
        otpCode == TestOtpCode && _hostEnvironment.IsDevelopment();

    private static string GenerateCode(int length)
    {
        var random = new Random();
        var min = (int)Math.Pow(10, length - 1);
        var max = (int)Math.Pow(10, length) - 1;
        return random.Next(min, max + 1).ToString();
    }
}
