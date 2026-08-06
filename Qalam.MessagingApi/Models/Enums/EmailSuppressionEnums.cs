namespace Qalam.MessagingApi.Models.Enums;

public enum EmailSuppressionReason
{
    HardBounce = 1,
    NoSuchUser = 2,
    OverQuota = 3,
    InvalidDomain = 4,
    Manual = 5,
    SyntheticLocal = 6
}

public enum EmailSuppressionSource
{
    SmtpSend = 1,
    BounceIngest = 2,
    Admin = 3,
    System = 4
}
