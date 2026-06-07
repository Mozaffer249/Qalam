namespace Qalam.Api.Helpers;

/// <summary>
/// Walks the multipart form on <c>POST /Teacher/SubmitRegistrationRequirements</c> and pulls
/// out the four open-ended dictionaries the admin-controlled catalog feeds into. Each parser
/// matches a single prefix and strips it to expose the raw <c>code</c>:
///
/// <list type="bullet">
///   <item><c>file_{code}</c>  → binary file (repeatable for multi-file)</item>
///   <item><c>text_{code}</c>  → free-form text value</item>
///   <item><c>bool_{code}</c>  → "true" / "false" / "1" / "0"</item>
///   <item><c>select_{code}</c> → option value (repeatable for multi-select)</item>
/// </list>
/// </summary>
public static class TeacherRegistrationFormHelper
{
    private const string FilePrefix = "file_";
    private const string TextPrefix = "text_";
    private const string BoolPrefix = "bool_";
    private const string SelectPrefix = "select_";

    public static Dictionary<string, List<IFormFile>> ParseCustomFilesByCode(HttpRequest request)
    {
        var result = new Dictionary<string, List<IFormFile>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in request.Form.Files)
        {
            if (!file.Name.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var code = file.Name[FilePrefix.Length..];
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

    public static Dictionary<string, string?> ParseTextValuesByCode(HttpRequest request)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in request.Form)
        {
            if (!key.StartsWith(TextPrefix, StringComparison.OrdinalIgnoreCase)) continue;
            var code = key[TextPrefix.Length..];
            if (string.IsNullOrWhiteSpace(code)) continue;
            result[code] = values.ToString();
        }
        return result;
    }

    public static Dictionary<string, bool?> ParseBoolValuesByCode(HttpRequest request)
    {
        var result = new Dictionary<string, bool?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in request.Form)
        {
            if (!key.StartsWith(BoolPrefix, StringComparison.OrdinalIgnoreCase)) continue;
            var code = key[BoolPrefix.Length..];
            if (string.IsNullOrWhiteSpace(code)) continue;

            var raw = values.ToString();
            if (bool.TryParse(raw, out var asBool))
                result[code] = asBool;
            else if (raw == "1") result[code] = true;
            else if (raw == "0") result[code] = false;
            else result[code] = null;
        }
        return result;
    }

    public static Dictionary<string, List<string>> ParseSelectionsByCode(HttpRequest request)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in request.Form)
        {
            if (!key.StartsWith(SelectPrefix, StringComparison.OrdinalIgnoreCase)) continue;
            var code = key[SelectPrefix.Length..];
            if (string.IsNullOrWhiteSpace(code)) continue;

            if (!result.TryGetValue(code, out var list))
            {
                list = new List<string>();
                result[code] = list;
            }
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v)) list.Add(v);
            }
        }
        return result;
    }
}
