using System.Text.Json;

namespace Qalam.Data.Helpers;

public static class RegistrationRequirementExtensionsHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static List<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string> { ".pdf", ".jpg", ".jpeg", ".png" };

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions)
                   ?? new List<string> { ".pdf", ".jpg", ".jpeg", ".png" };
        }
        catch
        {
            return new List<string> { ".pdf", ".jpg", ".jpeg", ".png" };
        }
    }

    public static string ToJson(IEnumerable<string> extensions) =>
        JsonSerializer.Serialize(extensions.Select(e => e.StartsWith('.') ? e : "." + e).Distinct().ToList(), JsonOptions);
}
