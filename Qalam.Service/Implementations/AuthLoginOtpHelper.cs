using Qalam.Data.Entity.Identity;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AuthLoginOtpHelper : IAuthLoginOtpHelper
{
    public string? ResolveRegistrationEmail(LoginOtp? loginOtp)
    {
        if (loginOtp == null)
            return null;

        if (!string.IsNullOrWhiteSpace(loginOtp.PendingEmail))
            return loginOtp.PendingEmail.Trim();

        if (loginOtp.Channel == LoginOtpChannel.Email
            && !string.IsNullOrWhiteSpace(loginOtp.DeliveryDestination)
            && loginOtp.DeliveryDestination.Contains('@', StringComparison.Ordinal))
        {
            return loginOtp.DeliveryDestination.Trim();
        }

        return null;
    }

    public string ResolveAccountEmail(string? registrationEmail, string fullPhoneNumber)
    {
        if (!string.IsNullOrWhiteSpace(registrationEmail))
            return registrationEmail.Trim();

        // Identity 8 + RequireUniqueEmail rejects null/empty email at user creation.
        var digits = new string(fullPhoneNumber.Where(char.IsDigit).ToArray());
        return $"phone_{digits}@phone.qalam.local";
    }

    public string? ResolveDeliveryEmail(LoginOtpEmailContext context)
    {
        if (!string.Equals(context.Settings.OtpDelivery, "Email", StringComparison.OrdinalIgnoreCase))
            return null;

        if (context.IsNewUser)
        {
            if (context.Settings.RegisterRequiresEmail && string.IsNullOrWhiteSpace(context.RequestEmail))
                return null;
            return NormalizeEmail(context.RequestEmail);
        }

        return NormalizeEmail(context.ExistingUserEmail);
    }

    public string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***@***";
        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length <= 2 ? local[0].ToString() : local[..2];
        return $"{visible}***@{domain}";
    }

    public string MaskPhone(string phoneNumber)
    {
        if (phoneNumber.Length < 4) return "****";
        return $"*******{phoneNumber[^4..]}";
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
