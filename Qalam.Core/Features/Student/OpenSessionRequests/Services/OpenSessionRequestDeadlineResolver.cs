using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Services;

/// <summary>
/// Resolves first-session start / request expiry for OSR create, publish, and draft update.
/// </summary>
public static class OpenSessionRequestDeadlineResolver
{
    public static async Task<DateTime?> ResolveFirstSessionStartUtcAsync(
        ApplicationDBContext db,
        IEnumerable<(DateOnly? PreferredDate, int? TimeSlotId)> sessions,
        CancellationToken cancellationToken = default)
    {
        var list = sessions.ToList();
        var slotIds = list
            .Where(s => s.TimeSlotId.HasValue && s.TimeSlotId.Value > 0)
            .Select(s => s.TimeSlotId!.Value)
            .Distinct()
            .ToList();

        Dictionary<int, TimeSpan> starts = new();
        if (slotIds.Count > 0)
        {
            starts = await db.TimeSlots
                .AsNoTracking()
                .Where(t => slotIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.StartTime, cancellationToken);
        }

        var tuples = list.Select(s =>
        {
            TimeSpan? start = null;
            if (s.TimeSlotId.HasValue && starts.TryGetValue(s.TimeSlotId.Value, out var ts))
                start = ts;
            return (s.PreferredDate, start);
        });

        return OpenSessionRequestExpiry.FirstSessionStartUtc(tuples);
    }

    public static async Task<DateTime?> ResolveFirstSessionStartUtcFromDtosAsync(
        ApplicationDBContext db,
        IEnumerable<CreateOpenSessionRequestSessionDto> sessions,
        CancellationToken cancellationToken = default)
    {
        return await ResolveFirstSessionStartUtcAsync(
            db,
            sessions.Select(s => ((DateOnly?)s.PreferredDate, (int?)s.TimeSlotId)),
            cancellationToken);
    }

    public static string? ValidateMinimumLead(
        DateTime nowUtc,
        DateTime? firstSessionStartUtc,
        OpenSessionRequestSettings settings,
        bool isTargeted)
    {
        if (firstSessionStartUtc == null)
            return "يجب تحديد تاريخ ووقت للجلسة الأولى";

        var leadHours = OpenSessionRequestExpiry.MinimumLeadHours(settings, isTargeted);
        var earliestAllowed = nowUtc.AddHours(Math.Max(0, leadHours));
        if (firstSessionStartUtc.Value < earliestAllowed)
        {
            return isTargeted
                ? $"للطلب الموجَّه يجب أن تكون الجلسة الأولى بعد {leadHours} ساعة على الأقل من الآن"
                : $"للطلب المنشور يجب أن تكون الجلسة الأولى بعد {leadHours} ساعة على الأقل من الآن";
        }

        return null;
    }

    public static DateTime ResolveExpiry(
        DateTime nowUtc,
        DateTime? requestedExpiresAt,
        DateTime? firstSessionStartUtc,
        OpenSessionRequestSettings settings,
        bool isTargeted)
    {
        return OpenSessionRequestExpiry.ResolveRequestExpiry(
            nowUtc,
            requestedExpiresAt,
            firstSessionStartUtc,
            settings,
            isTargeted);
    }
}
