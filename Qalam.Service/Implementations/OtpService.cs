using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class OtpService : IOtpService
{
    private readonly IPhoneOtpRepository _otpRepository;
    private readonly ILogger<OtpService> _logger;
    
    /// <summary>
    /// Test OTP code for development/testing purposes
    /// </summary>
    public const string TestOtpCode = "1234";

    public OtpService(
        IPhoneOtpRepository otpRepository,
        ILogger<OtpService> logger)
    {
        _otpRepository = otpRepository;
        _logger = logger;
    }

    public async Task<string> GeneratePhoneOtpAsync(string countryCode, string phoneNumber)
    {
        // Check if a valid OTP already exists
        var hasValidOtp = await _otpRepository.HasValidOtpAsync(phoneNumber);
        if (hasValidOtp)
        {
            _logger.LogWarning("Valid OTP already exists for phone {Phone}", phoneNumber);
            throw new InvalidOperationException(
                "A valid OTP code has already been sent. Please check your phone or wait 5 minutes to request a new one.");
        }

        // Remove expired OTPs for cleanup
        await _otpRepository.RemoveExpiredOtpsAsync(phoneNumber);
        await _otpRepository.SaveChangesAsync();

        // Generate 4-digit OTP
        var random = new Random();
        var otpCode = random.Next(1000, 9999).ToString();

        // Create and save OTP
        var otp = new PhoneConfirmationOtp
        {
            CountryCode = countryCode,
            PhoneNumber = phoneNumber,
            OtpCode = otpCode,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false
        };

        // AddAsync from GenericRepositoryAsync automatically saves changes
        await _otpRepository.AddAsync(otp);

        _logger.LogInformation("OTP generated for phone {Phone}", phoneNumber);
        
        return otpCode;
    }

    public async Task<bool> VerifyPhoneOtpAsync(string phoneNumber, string otpCode)
    {
        // Allow test code for development/testing
        if (otpCode == TestOtpCode)
        {
            _logger.LogInformation("Test OTP used for phone {Phone}", phoneNumber);
            return true;
        }
        
        var otp = await _otpRepository.GetValidOtpAsync(phoneNumber, otpCode);
        
        if (otp == null)
        {
            _logger.LogWarning("Invalid or expired OTP for phone {Phone}", phoneNumber);
            return false;
        }

        _logger.LogInformation("OTP verified successfully for phone {Phone}", phoneNumber);
        return true;
    }

    public async Task SendOtpSmsAsync(string fullPhoneNumber, string otpCode)
    {
        // TODO: Integrate with SMS provider (Twilio, AWS SNS, etc.)
        // For now, just log the OTP
        _logger.LogInformation(
            "SMS would be sent to {Phone} with OTP: {OTP}",
            fullPhoneNumber,
            otpCode);

        // In production, implement something like:
        // await _smsClient.SendAsync(fullPhoneNumber, $"Your verification code is: {otpCode}");
        
        await Task.CompletedTask;
    }
}
