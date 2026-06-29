using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Tests;

public class TeacherReviewSummaryCalculatorTests
{
    [Fact]
    public void FromDomainQuestionGroups_CountsOnlyRequiresAdminReviewItems()
    {
        var groups = new List<TeacherDomainQuestionGroupDto>
        {
            new()
            {
                DomainId = 1,
                DomainCode = "school",
                Questions =
                [
                    new TeacherDomainQuestionSubmissionStatusDto
                    {
                        RequiresAdminReview = true,
                        VerificationStatus = DocumentVerificationStatus.Pending,
                    },
                    new TeacherDomainQuestionSubmissionStatusDto
                    {
                        RequiresAdminReview = false,
                        VerificationStatus = DocumentVerificationStatus.Pending,
                    },
                    new TeacherDomainQuestionSubmissionStatusDto
                    {
                        RequiresAdminReview = true,
                        VerificationStatus = DocumentVerificationStatus.Approved,
                    },
                ],
            },
            new()
            {
                DomainId = 2,
                DomainCode = "quran",
                Questions =
                [
                    new TeacherDomainQuestionSubmissionStatusDto
                    {
                        RequiresAdminReview = true,
                        VerificationStatus = DocumentVerificationStatus.Rejected,
                    },
                ],
            },
        };

        var summary = TeacherReviewSummaryCalculator.FromDomainQuestionGroups(groups);

        Assert.Equal(1, summary.PendingDomainQuestions);
        Assert.Equal(1, summary.ApprovedDomainQuestions);
        Assert.Equal(1, summary.RejectedDomainQuestions);
    }

    [Fact]
    public void FromDomainQuestionGroups_EmptyGroups_ReturnsZeros()
    {
        var summary = TeacherReviewSummaryCalculator.FromDomainQuestionGroups([]);

        Assert.Equal(0, summary.PendingDomainQuestions);
        Assert.Equal(0, summary.ApprovedDomainQuestions);
        Assert.Equal(0, summary.RejectedDomainQuestions);
    }
}
