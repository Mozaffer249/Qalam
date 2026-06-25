using Microsoft.AspNetCore.Http;
using Moq;
using Qalam.Core.Features.Teacher.Commands.SubmitTeacherDomainQuestions;

namespace Qalam.Service.Tests;

public class TeacherDomainQuestionAnswerMapperTests
{
    [Fact]
    public void TryMap_MapsTextBooleanSelectionAndFiles()
    {
        var file = CreateMockFile("license.pdf");

        var answers = new List<TeacherDomainQuestionAnswerItem>
        {
            new() { Code = "school_experience_years", TextValue = "5" },
            new() { Code = "quran_has_ijaza", BoolValue = true },
            new() { Code = "pick_one", SelectedValues = ["a", "b"] },
            new() { Code = "school_teaching_license", Files = [file] }
        };

        var (input, error, codes) = TeacherDomainQuestionAnswerMapper.TryMap(1, answers);

        Assert.Null(error);
        Assert.Equal(4, codes.Count);
        Assert.Equal("5", input!.TextValuesByCode["school_experience_years"]);
        Assert.True(input.BoolValuesByCode["quran_has_ijaza"]);
        Assert.Equal(["a", "b"], input.SelectionsByCode["pick_one"]);
        Assert.Single(input.CustomFilesByCode["school_teaching_license"]);
    }

    [Fact]
    public void TryMap_RejectsDuplicateCodes()
    {
        var answers = new List<TeacherDomainQuestionAnswerItem>
        {
            new() { Code = "school_experience_years", TextValue = "5" },
            new() { Code = "school_experience_years", TextValue = "6" }
        };

        var (_, error, _) = TeacherDomainQuestionAnswerMapper.TryMap(1, answers);

        Assert.NotNull(error);
        Assert.Contains("Duplicate", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryMap_IgnoresEmptyCodeRows()
    {
        var answers = new List<TeacherDomainQuestionAnswerItem>
        {
            new() { Code = "", TextValue = "ignored" },
            new() { Code = "school_experience_years", TextValue = "5" }
        };

        var (input, error, codes) = TeacherDomainQuestionAnswerMapper.TryMap(1, answers);

        Assert.Null(error);
        Assert.Single(codes);
        Assert.Equal("5", input!.TextValuesByCode["school_experience_years"]);
    }

    [Fact]
    public void TryMap_RequiresAtLeastOneAnswer()
    {
        var (_, error, _) = TeacherDomainQuestionAnswerMapper.TryMap(1, []);

        Assert.NotNull(error);
        Assert.Contains("At least one answer", error, StringComparison.OrdinalIgnoreCase);
    }

    private static IFormFile CreateMockFile(string fileName)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(100);
        return file.Object;
    }
}
