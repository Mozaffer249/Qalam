namespace Qalam.Data.Helpers;

/// <summary>
/// Cooldown for "new chat message" emails so burst sends do not spam the other party.
/// </summary>
public class ChatEmailSettings
{
    public const string SectionName = "ChatEmailSettings";

    /// <summary>
    /// Minimum minutes between chat emails for the same conversation + recipient.
    /// 0 disables the cooldown (every message emails).
    /// </summary>
    public int CooldownMinutes { get; set; } = 10;
}
