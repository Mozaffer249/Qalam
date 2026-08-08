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

    public static byte[] BuildCsvBytes(IReadOnlyList<AdminTeacherListItemDto> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",",
        [
            "TeacherId",
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
        ]));

        foreach (var t in items)
        {
            var answers = FlattenDomainAnswers(t.DomainQuestionSubmissions);
            var answersJson = JsonSerializer.Serialize(t.DomainQuestionSubmissions, JsonOptions);
            sb.AppendLine(string.Join(",",
            [
                Csv(t.TeacherId.ToString()),
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
            ]));
        }

        var bom = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var content = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, content, 0, bom.Length);
        Buffer.BlockCopy(body, 0, content, bom.Length, body.Length);
        return content;
    }

    private static string Csv(string? value)
    {
        var v = value ?? "";
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
