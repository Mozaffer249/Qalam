using Microsoft.AspNetCore.Http;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherDomainQuestions;

public static class TeacherDomainQuestionAnswerMapper
{
    public static (TeacherDomainQuestionSubmissionInput? Input, string? Error, List<string> SubmittedCodes) TryMap(
        int domainId,
        IReadOnlyList<TeacherDomainQuestionAnswerItem> answers)
    {
        var nonEmpty = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Code))
            .ToList();

        if (nonEmpty.Count == 0)
            return (null, "At least one answer with a non-empty code is required.", []);

        var duplicate = nonEmpty
            .GroupBy(a => a.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            return (null, $"Duplicate answer code '{duplicate.Key}'.", []);

        var customFiles = new Dictionary<string, List<IFormFile>>(StringComparer.OrdinalIgnoreCase);
        var textValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var boolValues = new Dictionary<string, bool?>(StringComparer.OrdinalIgnoreCase);
        var selections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var submittedCodes = new List<string>();

        foreach (var answer in nonEmpty)
        {
            var code = answer.Code.Trim();
            submittedCodes.Add(code);

            if (answer.Files.Count > 0)
                customFiles[code] = answer.Files.Where(f => f.Length > 0).ToList();

            if (!string.IsNullOrWhiteSpace(answer.TextValue))
                textValues[code] = answer.TextValue.Trim();

            if (answer.BoolValue.HasValue)
                boolValues[code] = answer.BoolValue;

            if (answer.SelectedValues.Count > 0)
            {
                selections[code] = answer.SelectedValues
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .ToList();
            }
        }

        return (new TeacherDomainQuestionSubmissionInput
        {
            DomainId = domainId,
            CustomFilesByCode = customFiles,
            TextValuesByCode = textValues,
            BoolValuesByCode = boolValues,
            SelectionsByCode = selections
        }, null, submittedCodes);
    }
}
