namespace Qalam.Data.Entity.Common;

/// <summary>Admin workflow status for contact form messages.</summary>
public static class ContactMessageStatus
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string Closed = "Closed";

    public static readonly IReadOnlyList<string> All =
    [
        Open,
        InProgress,
        Closed
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status);
}
