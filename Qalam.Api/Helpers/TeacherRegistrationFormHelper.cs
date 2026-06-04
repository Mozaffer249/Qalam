namespace Qalam.Api.Helpers;

/// <summary>
/// Parses multipart files named <c>file_{requirementCode}</c> for custom registration requirements.
/// </summary>
public static class TeacherRegistrationFormHelper
{
    private const string Prefix = "file_";

    public static Dictionary<string, List<IFormFile>> ParseCustomFilesByCode(HttpRequest request)
    {
        var result = new Dictionary<string, List<IFormFile>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in request.Form.Files)
        {
            if (!file.Name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var code = file.Name[Prefix.Length..];
            if (string.IsNullOrWhiteSpace(code))
                continue;

            if (!result.TryGetValue(code, out var list))
            {
                list = new List<IFormFile>();
                result[code] = list;
            }

            list.Add(file);
        }

        return result;
    }
}
