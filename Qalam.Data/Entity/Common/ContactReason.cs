namespace Qalam.Data.Entity.Common;

/// <summary>Fixed reason codes for public contact form submissions.</summary>
public static class ContactReason
{
    public const string GeneralInquiry = "GeneralInquiry";
    public const string TechnicalSupport = "TechnicalSupport";
    public const string Partnership = "Partnership";
    public const string TeachingApplication = "TeachingApplication";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All =
    [
        GeneralInquiry,
        TechnicalSupport,
        Partnership,
        TeachingApplication,
        Other
    ];

    public static bool IsValid(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && All.Contains(reason);
}
