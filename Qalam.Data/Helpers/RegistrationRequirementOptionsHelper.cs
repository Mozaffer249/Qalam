using System.Text.Json;

namespace Qalam.Data.Helpers;

/// <summary>One bilingual choice on a Selection requirement.</summary>
public sealed record RequirementOption(string Value, string LabelAr, string LabelEn);

/// <summary>
/// Parse / serialize the JSON blob stored in <c>TeacherRegistrationRequirement.OptionsJson</c>.
/// Mirrors the <c>RegistrationRequirementExtensionsHelper</c> pattern.
/// </summary>
public static class RegistrationRequirementOptionsHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static List<RequirementOption> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<RequirementOption>();

        try
        {
            return JsonSerializer.Deserialize<List<RequirementOption>>(json, JsonOptions)
                   ?? new List<RequirementOption>();
        }
        catch
        {
            return new List<RequirementOption>();
        }
    }

    public static string? Serialize(IEnumerable<RequirementOption>? options)
    {
        if (options == null) return null;
        var list = options.ToList();
        return list.Count == 0 ? null : JsonSerializer.Serialize(list, JsonOptions);
    }
}
