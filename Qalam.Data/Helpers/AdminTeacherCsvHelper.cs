using System.Text;
using System.Text.Json;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Data.Helpers;

public static class AdminTeacherCsvHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string FormatAnswerValue(TeacherDomainQuestionSubmissionStatusDto q)
    {
        if (q.SelectedOptions is { Count: > 0 })
        {
            return string.Join(
                ",",
                q.SelectedOptions.Select(o =>
                    !string.IsNullOrWhiteSpace(o.LabelEn) ? o.LabelEn
                    : !string.IsNullOrWhiteSpace(o.LabelAr) ? o.LabelAr
                    : o.Value));
        }

        if (q.BoolValue.HasValue)
            return q.BoolValue.Value ? "true" : "false";

        return q.TextValue?.Trim() ?? "";
    }

    public static string FlattenDomainAnswers(IEnumerable<TeacherDomainQuestionGroupDto> groups)
    {
        var parts = new List<string>();
        foreach (var g in groups)
        {
            foreach (var q in g.Questions)
            {
                var value = FormatAnswerValue(q);
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                parts.Add($"{g.DomainCode}.{q.Code}={value}");
            }
        }

        return string.Join(" | ", parts);
    }

    public static string BuildDomainAnswersSummary(IEnumerable<TeacherDomainQuestionGroupDto> groups)
    {
        var parts = new List<string>();
        foreach (var g in groups)
        {
            foreach (var q in g.Questions)
            {
                var value = FormatAnswerValue(q);
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                parts.Add($"{g.DomainCode}:{q.Code}={value}");
            }
        }

        return string.Join("; ", parts);
    }

    public static string FormatRequirementStatus(TeacherRegistrationSubmissionStatusDto req)
    {
        if (!req.IsSubmitted)
            return "NotSubmitted";
        return req.VerificationStatus?.ToString() ?? "Pending";
    }

    public static string FormatRequirementValue(TeacherRegistrationSubmissionStatusDto req)
    {
        if (!req.IsSubmitted)
            return "";

        if (string.Equals(req.RequirementType, "Selection", StringComparison.OrdinalIgnoreCase)
            && req.SelectedOptions is { Count: > 0 })
        {
            return string.Join(
                ",",
                req.SelectedOptions.Select(o =>
                    !string.IsNullOrWhiteSpace(o.LabelEn) ? o.LabelEn
                    : !string.IsNullOrWhiteSpace(o.LabelAr) ? o.LabelAr
                    : o.Value));
        }

        if (string.Equals(req.RequirementType, "Boolean", StringComparison.OrdinalIgnoreCase)
            && req.BoolValue.HasValue)
            return req.BoolValue.Value ? "true" : "false";

        if (string.Equals(req.RequirementType, "Text", StringComparison.OrdinalIgnoreCase))
            return req.TextValue?.Trim() ?? "";

        if (string.Equals(req.RequirementType, "File", StringComparison.OrdinalIgnoreCase))
        {
            if (req.TeacherDocumentId.HasValue)
                return req.TeacherDocumentId.Value.ToString();
            return req.IsSubmitted ? "submitted" : "";
        }

        if (req.BoolValue.HasValue)
            return req.BoolValue.Value ? "true" : "false";

        return req.TextValue?.Trim() ?? "";
    }

    public static string BuildRegistrationRequirementsSummary(
        IEnumerable<TeacherRegistrationSubmissionStatusDto> requirements)
    {
        return string.Join(
            "; ",
            requirements.Select(r => $"{r.Code}={FormatRequirementStatus(r)}"));
    }

    public static byte[] BuildCsvBytes(IReadOnlyList<AdminTeacherListItemDto> items)
    {
        var requirementCodes = DiscoverRequirementCodes(items);

        var headers = new List<string>
        {
            "TeacherId",
            "UserId",
            "FullName",
            "Phone",
            "Email",
            "Status",
            "Location",
            "Nationality",
            "CreatedAt",
            "SelectedDomainCodes",
            "SelectedDomainNamesAr",
            "SelectedDomainNamesEn",
            "SubjectNamesAr",
            "SubjectNamesEn",
            "CertificateTitles",
            "TotalDocuments",
            "PendingDocuments",
            "ApprovedDocuments",
            "RejectedDocuments",
            "DomainAnswers",
            "DomainAnswersJson"
        };

        foreach (var code in requirementCodes)
        {
            headers.Add($"Req_{code}_status");
            headers.Add($"Req_{code}_value");
        }

        headers.Add("RegistrationRequirementsSummary");
        headers.Add("RegistrationRequirementsJson");

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Csv)));

        foreach (var t in items)
        {
            var answers = FlattenDomainAnswers(t.DomainQuestionSubmissions);
            var answersJson = JsonSerializer.Serialize(t.DomainQuestionSubmissions, JsonOptions);
            var reqByCode = (t.RegistrationRequirements ?? [])
                .GroupBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var cells = new List<string>
            {
                Csv(t.TeacherId.ToString()),
                Csv(t.UserId.ToString()),
                Csv(t.FullName),
                Csv(t.PhoneNumber),
                Csv(t.Email),
                Csv(t.Status),
                Csv(t.Location?.ToString()),
                Csv(t.Nationality),
                Csv(t.CreatedAt.ToString("o")),
                Csv(t.SelectedDomainCodes),
                Csv(t.SelectedDomainNamesAr),
                Csv(t.SelectedDomainNamesEn),
                Csv(t.SubjectNamesAr),
                Csv(t.SubjectNamesEn),
                Csv(t.CertificateTitles),
                Csv(t.TotalDocuments.ToString()),
                Csv(t.PendingDocuments.ToString()),
                Csv(t.ApprovedDocuments.ToString()),
                Csv(t.RejectedDocuments.ToString()),
                Csv(answers),
                Csv(answersJson)
            };

            foreach (var code in requirementCodes)
            {
                if (reqByCode.TryGetValue(code, out var req))
                {
                    cells.Add(Csv(FormatRequirementStatus(req)));
                    cells.Add(Csv(FormatRequirementValue(req)));
                }
                else
                {
                    cells.Add(Csv("NotSubmitted"));
                    cells.Add(Csv(""));
                }
            }

            cells.Add(Csv(BuildRegistrationRequirementsSummary(t.RegistrationRequirements ?? [])));
            cells.Add(Csv(JsonSerializer.Serialize(t.RegistrationRequirements ?? [], JsonOptions)));

            sb.AppendLine(string.Join(",", cells));
        }

        var bom = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var content = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, content, 0, bom.Length);
        Buffer.BlockCopy(body, 0, content, bom.Length, body.Length);
        return content;
    }

    /// <summary>
    /// Prefer the first row's checklist order (active catalog order). Fall back to a stable
    /// union of codes across all rows when the first row is empty.
    /// </summary>
    private static List<string> DiscoverRequirementCodes(IReadOnlyList<AdminTeacherListItemDto> items)
    {
        var firstWithReqs = items.FirstOrDefault(i => i.RegistrationRequirements is { Count: > 0 });
        if (firstWithReqs != null)
            return firstWithReqs.RegistrationRequirements.Select(r => r.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return items
            .SelectMany(i => i.RegistrationRequirements ?? [])
            .Select(r => r.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Csv(string? value)
    {
        var v = value ?? "";
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
