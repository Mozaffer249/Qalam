using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Live;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class LiveSessionAccessService : ILiveSessionAccessService
{
    private readonly ISessionPresenceService _presenceService;
    private readonly ILiveSessionProvider _liveSessionProvider;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ApplicationDBContext _db;
    private readonly LiveSessionSettings _settings;
    private readonly ILogger<LiveSessionAccessService> _logger;

    public LiveSessionAccessService(
        ISessionPresenceService presenceService,
        ILiveSessionProvider liveSessionProvider,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        ApplicationDBContext db,
        IOptions<LiveSessionSettings> settings,
        ILogger<LiveSessionAccessService> logger)
    {
        _presenceService = presenceService;
        _liveSessionProvider = liveSessionProvider;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _db = db;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<(bool Ok, string Message, bool Forbidden, bool NotFound, bool Unavailable, LiveSessionAccessDto? Access)>
        GetTeacherAccessAsync(int userId, int courseScheduleId, CancellationToken cancellationToken = default)
    {
        if (!IsProviderReady())
            return UnavailableResult();

        var (ok, message, forbidden, notFound) = await _presenceService.JoinAsTeacherAsync(
            userId, courseScheduleId, cancellationToken);
        if (forbidden) return (false, message, true, false, false, null);
        if (notFound) return (false, message, false, true, false, null);
        if (!ok) return (false, message, false, false, false, null);

        var teacher = await _teacherRepository.GetByUserIdAsync(userId);
        if (teacher == null)
            return (false, "Teacher profile not found.", false, true, false, null);

        var displayName = await ResolveUserDisplayNameAsync(teacher.UserId, $"Teacher {teacher.Id}", cancellationToken);
        return await MintAsync(
            courseScheduleId,
            identity: $"teacher-{teacher.Id}",
            displayName,
            role: "teacher",
            cancellationToken);
    }

    public async Task<(bool Ok, string Message, bool Forbidden, bool NotFound, bool Unavailable, LiveSessionAccessDto? Access)>
        GetStudentAccessAsync(int userId, int courseScheduleId, CancellationToken cancellationToken = default)
    {
        if (!IsProviderReady())
            return UnavailableResult();

        var (ok, message, forbidden, notFound) = await _presenceService.JoinAsStudentAsync(
            userId, courseScheduleId, cancellationToken);
        if (forbidden) return (false, message, true, false, false, null);
        if (notFound) return (false, message, false, true, false, null);
        if (!ok) return (false, message, false, false, false, null);

        var student = await _studentRepository.GetByUserIdAsync(userId);
        if (student == null)
            return (false, "Student profile not found.", false, true, false, null);

        var displayName = await ResolveUserDisplayNameAsync(student.UserId, $"Student {student.Id}", cancellationToken);
        return await MintAsync(
            courseScheduleId,
            identity: $"student-{student.Id}",
            displayName,
            role: "student",
            cancellationToken);
    }

    private async Task<(bool Ok, string Message, bool Forbidden, bool NotFound, bool Unavailable, LiveSessionAccessDto? Access)>
        MintAsync(
            int courseScheduleId,
            string identity,
            string displayName,
            string role,
            CancellationToken cancellationToken)
    {
        try
        {
            var ttlMinutes = Math.Max(1, _settings.LiveKit.TokenTtlMinutes);
            var access = await _liveSessionProvider.CreateAccessAsync(
                new LiveSessionAccessRequest
                {
                    RoomName = LiveSessionRoomNames.ForSchedule(courseScheduleId),
                    Identity = identity,
                    DisplayName = displayName,
                    Role = role,
                    Ttl = TimeSpan.FromMinutes(ttlMinutes),
                },
                cancellationToken);

            return (true, "Live session access granted.", false, false, false, access);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Live session provider unavailable for schedule {ScheduleId}.", courseScheduleId);
            return (false, ex.Message, false, false, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mint live session token for schedule {ScheduleId}.", courseScheduleId);
            return (false, "Failed to create live session access.", false, false, true, null);
        }
    }

    private bool IsProviderReady()
    {
        var provider = _settings.Provider ?? string.Empty;
        if (!provider.Equals(LiveKitLiveSessionProvider.Name, StringComparison.OrdinalIgnoreCase))
            return false;

        var lk = _settings.LiveKit;
        return !string.IsNullOrWhiteSpace(lk.Url)
               && !string.IsNullOrWhiteSpace(lk.ApiKey)
               && !string.IsNullOrWhiteSpace(lk.ApiSecret);
    }

    private static (bool Ok, string Message, bool Forbidden, bool NotFound, bool Unavailable, LiveSessionAccessDto? Access)
        UnavailableResult()
        => (false,
            "Live session provider is not configured. Set LiveSession settings (Provider + LiveKit Url/ApiKey/ApiSecret).",
            false, false, true, null);

    private async Task<string> ResolveUserDisplayNameAsync(
        int? userId,
        string fallback,
        CancellationToken cancellationToken)
    {
        if (userId is null or <= 0)
            return fallback;

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return fallback;

        var name = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}".Trim();
        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }
}
