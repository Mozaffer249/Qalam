using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Admin;

public class TeacherReviewSummaryDto
{
    public int PendingDomainQuestions { get; set; }
    public int ApprovedDomainQuestions { get; set; }
    public int RejectedDomainQuestions { get; set; }
}

public static class TeacherReviewSummaryCalculator
{
    public static TeacherReviewSummaryDto FromDomainQuestionGroups(
        IEnumerable<TeacherDomainQuestionGroupDto> groups)
    {
        var reviewItems = groups
            .SelectMany(g => g.Questions)
            .Where(q => q.RequiresAdminReview)
            .ToList();

        return new TeacherReviewSummaryDto
        {
            PendingDomainQuestions = reviewItems.Count(q =>
                q.VerificationStatus == DocumentVerificationStatus.Pending),
            ApprovedDomainQuestions = reviewItems.Count(q =>
                q.VerificationStatus == DocumentVerificationStatus.Approved),
            RejectedDomainQuestions = reviewItems.Count(q =>
                q.VerificationStatus == DocumentVerificationStatus.Rejected),
        };
    }
}
