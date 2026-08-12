namespace Qalam.Data.Entity.Common.Enums;

/// <summary>
/// Inbox scope for GET /Student/Invitations (Active = pending/actionable, Archived = history).
/// </summary>
public enum InvitationInboxScope
{
    Active = 1,
    Archived = 2
}
