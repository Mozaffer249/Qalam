namespace Qalam.Data.DTOs.Account;

public class NotificationPreferencesDto
{
    public bool PushEnabled { get; set; } = true;
    public bool EmailDigestEnabled { get; set; } = true;
    public bool SmsAlertsEnabled { get; set; } = false;
}

public class RegisterDeviceTokenDto
{
    public string Token { get; set; } = null!;
    /// <summary>ios | android | web</summary>
    public string Platform { get; set; } = null!;
    public string? AppVersion { get; set; }
}

public class UnregisterDeviceTokenDto
{
    public string Token { get; set; } = null!;
}

public class DeactivateAccountDto
{
    public string Password { get; set; } = null!;
}

/// <summary>Machine-readable reasons when account deactivation is blocked.</summary>
public static class AccountDeactivationBlockingReasons
{
    public const string ActiveEnrollment = "ActiveEnrollment";
    public const string OpenSessionRequest = "OpenSessionRequest";
    public const string PendingInvitation = "PendingInvitation";
}

public class AccountDeactivationBlockedDto
{
    public List<string> BlockingReasons { get; set; } = new();
}
