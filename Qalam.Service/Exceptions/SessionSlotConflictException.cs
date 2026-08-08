using Qalam.Data.DTOs.OpenSessionRequests;

namespace Qalam.Service.Exceptions;

/// <summary>
/// Thrown when accepting an OSR offer would occupy teacher slots that are already booked.
/// </summary>
public class SessionSlotConflictException : Exception
{
    public SessionSlotConflictException(IReadOnlyList<SessionAvailabilityMatchDto> blockedSessions)
        : base("SCHEDULE_CONFLICT")
    {
        BlockedSessions = blockedSessions;
    }

    public IReadOnlyList<SessionAvailabilityMatchDto> BlockedSessions { get; }
}
