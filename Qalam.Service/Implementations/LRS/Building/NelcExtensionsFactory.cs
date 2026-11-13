using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;

namespace Qalam.Service.Implementations.LRS.Building;

/// <summary>
/// Assembles the NELC context-extension dictionaries so builders never hand-roll the IRIs or shapes.
/// </summary>
public static class NelcExtensionsFactory
{
    private static Dictionary<string, object> PlatformName(LrsSettings s) => new()
    {
        [NelcExtensions.Platform] = new Dictionary<string, object>
        {
            ["name"] = new Dictionary<string, string>
            {
                ["ar-SA"] = s.PlatformNameAr,
                ["en-US"] = s.PlatformNameEn
            }
        }
    };

    /// <summary>Extensions for the <c>registered</c> statement: duration, lms/program urls, platform name.</summary>
    public static Dictionary<string, object> Registration(LrsSettings s, Course course, string totalDurationIso)
    {
        var ext = new Dictionary<string, object>
        {
            [NelcExtensions.Duration] = totalDurationIso,
            [NelcExtensions.LmsUrl] = s.PlatformIri,
            [NelcExtensions.ProgramUrl] = $"{s.PlatformIri}/course/{course.Id}"
        };
        foreach (var kv in PlatformName(s))
            ext[kv.Key] = kv.Value;
        return ext;
    }

    /// <summary>Extensions carrying only the platform name (used by <c>initialized</c>).</summary>
    public static Dictionary<string, object> Platform(LrsSettings s) => PlatformName(s);

    /// <summary>
    /// Extensions for the <c>earned</c> statement: certificate location + platform name. The location
    /// must be a reachable URL (NELC fetches it — CRT002), so it points at the public certificate endpoint.
    /// </summary>
    public static Dictionary<string, object> Certificate(LrsSettings s, int enrollmentId, string certificateBaseUrl)
    {
        var baseUrl = (string.IsNullOrWhiteSpace(certificateBaseUrl) ? s.PlatformIri : certificateBaseUrl)
            .TrimEnd('/');

        var ext = new Dictionary<string, object>
        {
            [NelcExtensions.CertificateLocation] = $"{baseUrl}/Api/V1/Lrs/Certificate/{enrollmentId}"
        };
        foreach (var kv in PlatformName(s))
            ext[kv.Key] = kv.Value;
        return ext;
    }
}
