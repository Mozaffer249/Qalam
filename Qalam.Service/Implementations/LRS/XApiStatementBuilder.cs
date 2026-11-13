using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations.LRS.Building;

namespace Qalam.Service.Implementations.LRS;

/// <summary>
/// Thin orchestrator: composes each NELC statement from the reusable builders in
/// <c>LRS/Building/</c> (actor, activity, context, extensions, duration, id). The NELC validator
/// rules live in those helpers — CRS002 (no contextActivities on course objects) in
/// <see cref="NelcContextFactory"/>, CRS004 (ISO-8601 duration) in <see cref="XApiDuration"/>,
/// SEQ003 (deterministic ids) in <see cref="XApiStatementId"/>.
/// </summary>
public class XApiStatementBuilder : IXApiStatementBuilder
{
    private readonly ApplicationDBContext _db;
    private readonly LrsSettings _fallback;
    private LrsSettings? _effective;

    public XApiStatementBuilder(ApplicationDBContext db, IOptions<LrsSettings> fallback)
    {
        _db = db;
        _fallback = fallback.Value;
    }

    private LrsSettings Settings => _effective ?? _fallback;

    public async Task LoadSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var row = await _db.Set<Data.Entity.Common.SystemSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == "LrsCredentials", ct);
            if (row != null && !string.IsNullOrEmpty(row.Value))
            {
                var doc = JsonSerializer.Deserialize<JsonElement>(row.Value);
                _effective = new LrsSettings
                {
                    Enabled = doc.TryGetProperty("enabled", out var e) && e.GetBoolean(),
                    Endpoint = doc.TryGetProperty("endpoint", out var ep) ? ep.GetString() ?? "" : "",
                    Key = doc.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "",
                    Secret = doc.TryGetProperty("secret", out var s) ? s.GetString() ?? "" : "",
                    MaxRetries = doc.TryGetProperty("maxRetries", out var mr) ? mr.GetInt32() : _fallback.MaxRetries,
                    PlatformIri = doc.TryGetProperty("platformIri", out var pi) && !string.IsNullOrEmpty(pi.GetString()) ? pi.GetString()! : _fallback.PlatformIri,
                    XApiVersion = doc.TryGetProperty("xApiVersion", out var xv) && !string.IsNullOrEmpty(xv.GetString()) ? xv.GetString()! : _fallback.XApiVersion,
                    PlatformKey = doc.TryGetProperty("platformKey", out var pk) && !string.IsNullOrEmpty(pk.GetString()) ? pk.GetString()! : _fallback.PlatformKey,
                    PlatformNameAr = doc.TryGetProperty("platformNameAr", out var pna) && !string.IsNullOrEmpty(pna.GetString()) ? pna.GetString()! : _fallback.PlatformNameAr,
                    PlatformNameEn = doc.TryGetProperty("platformNameEn", out var pne) && !string.IsNullOrEmpty(pne.GetString()) ? pne.GetString()! : _fallback.PlatformNameEn,
                };
            }
        }
        catch { /* fall through to defaults */ }
    }

    public XApiStatement BuildRegistered(User student, Course course, Enrollment enrollment, User? teacher, string? identityNumber = null)
    {
        var totalMinutes = enrollment.CourseSchedules?.Sum(cs => cs.DurationMinutes) ?? 0;
        var extensions = NelcExtensionsFactory.Registration(Settings, course, XApiDuration.FromMinutes(totalMinutes));

        return Compose(
            XApiActorFactory.Agent(Settings, student, identityNumber),
            XApiVerbs.Create(XApiVerbs.Registered, "registered"),
            NelcActivityFactory.Course(Settings, course),
            NelcContextFactory.Course(Settings, teacher, extensions),
            result: null);
    }

    public XApiStatement BuildInitialized(User student, Course course, User? teacher, string? identityNumber = null)
    {
        return Compose(
            XApiActorFactory.Agent(Settings, student, identityNumber),
            XApiVerbs.Create(XApiVerbs.Initialized, "initialized"),
            NelcActivityFactory.Course(Settings, course),
            NelcContextFactory.Course(Settings, teacher, NelcExtensionsFactory.Platform(Settings)),
            result: null);
    }

    public XApiStatement BuildProgressUpdated(User student, Course course, Enrollment enrollment, int completedSessions, int totalSessions, string? identityNumber = null)
    {
        var scaled = totalSessions > 0 ? Math.Round((decimal)completedSessions / totalSessions, 4) : 0m;
        var result = new XApiResult
        {
            Score = new XApiScore { Scaled = scaled },
            Completion = completedSessions >= totalSessions
        };

        // Each distinct progress point is a distinct statement, so the completed count joins the id.
        return Compose(
            XApiActorFactory.Agent(Settings, student, identityNumber),
            XApiVerbs.Create(XApiVerbs.Progressed, "progressed"),
            NelcActivityFactory.Course(Settings, course),
            NelcContextFactory.Course(Settings),
            result,
            idDiscriminator: completedSessions.ToString());
    }

    public XApiStatement BuildCourseCompleted(User student, Course course, Enrollment enrollment, string? identityNumber = null)
    {
        return Compose(
            XApiActorFactory.Agent(Settings, student, identityNumber),
            XApiVerbs.Create(XApiVerbs.Completed, "completed"),
            NelcActivityFactory.Course(Settings, course),
            NelcContextFactory.Course(Settings),
            result: null);
    }

    public XApiStatement BuildRated(User student, Course course, int rating, string? feedback, string? identityNumber = null)
    {
        var result = new XApiResult
        {
            Score = new XApiScore
            {
                Scaled = Math.Round(rating / 5m, 4),
                Raw = rating,
                Min = 0,
                Max = 5
            },
            Response = string.IsNullOrWhiteSpace(feedback) ? null : feedback
        };

        return Compose(
            XApiActorFactory.Agent(Settings, student, identityNumber),
            XApiVerbs.Create(XApiVerbs.Rated, "rated"),
            NelcActivityFactory.Course(Settings, course),
            NelcContextFactory.Course(Settings),
            result);
    }

    public XApiStatement BuildCourseEarned(User student, Course course, Enrollment enrollment, string? identityNumber = null)
    {
        var extensions = NelcExtensionsFactory.Certificate(Settings, enrollment.Id);

        return Compose(
            XApiActorFactory.Agent(Settings, student, identityNumber),
            XApiVerbs.Create(XApiVerbs.Earned, "earned"),
            NelcActivityFactory.Certificate(Settings, course, enrollment.Id),
            NelcContextFactory.Certificate(Settings, course, extensions),
            result: null);
    }

    private XApiStatement Compose(
        XApiActor actor,
        XApiVerb verb,
        XApiObject @object,
        XApiContext context,
        XApiResult? result,
        string? idDiscriminator = null)
    {
        var learner = actor.Name ?? actor.Mbox ?? actor.Account?.Name ?? string.Empty;
        var id = idDiscriminator == null
            ? XApiStatementId.Deterministic(Settings.PlatformIri, learner, verb.Id, @object.Id)
            : XApiStatementId.Deterministic(Settings.PlatformIri, learner, verb.Id, @object.Id, idDiscriminator);

        return new XApiStatement
        {
            Id = id,
            Actor = actor,
            Verb = verb,
            Object = @object,
            Result = result,
            Context = context,
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }
}
