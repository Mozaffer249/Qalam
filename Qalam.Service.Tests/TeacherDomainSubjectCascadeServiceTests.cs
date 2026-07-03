using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherDomainSubjectCascadeServiceTests
{
    private const int TeacherId = 3;
    private const int DomainId = 1;
    private const int AdminId = 9;

    [Fact]
    public async Task RejectSubjectsInDomain_SetsCascadeRejectionOnNonRejectedSubjects()
    {
        var subject = new TeacherSubject
        {
            Id = 10,
            TeacherId = TeacherId,
            SubjectId = 12,
            VerificationStatus = DocumentVerificationStatus.Pending,
            IsActive = true,
            Subject = new Subject { DomainId = DomainId, NameEn = "Math", NameAr = "رياضيات" }
        };

        var teacherSubjectRepo = new Mock<ITeacherSubjectRepository>();
        teacherSubjectRepo
            .Setup(r => r.GetSubjectsInDomainForCascadeRejectAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([subject]);

        var service = new TeacherDomainSubjectCascadeService(
            teacherSubjectRepo.Object,
            Mock.Of<ITeacherDomainQuestionRepository>(),
            Mock.Of<ITeacherDomainQuestionSubmissionRepository>(),
            NullLogger<TeacherDomainSubjectCascadeService>.Instance);

        await service.RejectSubjectsInDomainAsync(TeacherId, DomainId, AdminId, "Invalid license");

        Assert.Equal(DocumentVerificationStatus.Rejected, subject.VerificationStatus);
        Assert.Equal(TeacherSubjectRejectionSource.DomainQuestionCascade, subject.RejectionSource);
        Assert.False(subject.IsActive);
        teacherSubjectRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveSubjectsInDomain_SetsApproved_WhenDomainFullyApproved()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "license",
            IsRequired = true,
            RequiresAdminReview = true
        };

        var subject = new TeacherSubject
        {
            Id = 10,
            TeacherId = TeacherId,
            VerificationStatus = DocumentVerificationStatus.Rejected,
            RejectionSource = TeacherSubjectRejectionSource.DomainQuestionCascade,
            IsActive = false,
            Subject = new Subject { DomainId = DomainId }
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherAndDomainIdAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Approved
                }
            ]);

        var teacherSubjectRepo = new Mock<ITeacherSubjectRepository>();
        teacherSubjectRepo
            .Setup(r => r.GetTeacherSubjectsInDomainAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([subject]);

        var service = new TeacherDomainSubjectCascadeService(
            teacherSubjectRepo.Object,
            questionRepo.Object,
            submissionRepo.Object,
            NullLogger<TeacherDomainSubjectCascadeService>.Instance);

        await service.ApproveSubjectsInDomainAsync(TeacherId, DomainId);

        Assert.Equal(DocumentVerificationStatus.Approved, subject.VerificationStatus);
        Assert.Null(subject.RejectionSource);
        Assert.True(subject.IsActive);
    }

    [Fact]
    public async Task GetSubjectSaveBlockReason_ReturnsPendingMessage_WhenDomainNotFullyApproved()
    {
        var question = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            IsRequired = true
        };

        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherAndDomainIdAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Pending
                }
            ]);

        var service = new TeacherDomainSubjectCascadeService(
            Mock.Of<ITeacherSubjectRepository>(),
            questionRepo.Object,
            submissionRepo.Object,
            NullLogger<TeacherDomainSubjectCascadeService>.Instance);

        var reason = await service.GetSubjectSaveBlockReasonForDomainAsync(
            TeacherId, DomainId, "School", "school");

        Assert.Contains("wait for approval", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IsDomainFullyApproved_ReturnsFalse_WhenOptionalAdminReviewPending()
    {
        var required = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            Code = "years",
            IsRequired = true,
            RequiresAdminReview = false
        };
        var optional = new TeacherDomainQuestion
        {
            Id = 2,
            DomainId = DomainId,
            Code = "cert",
            IsRequired = false,
            RequiresAdminReview = true
        };

        var service = BuildCascadeService(
            [required, optional],
            [
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Approved
                },
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 2,
                    VerificationStatus = DocumentVerificationStatus.Pending
                }
            ]);

        Assert.False(await service.IsDomainFullyApprovedForTeacherAsync(TeacherId, DomainId));
    }

    [Fact]
    public async Task IsDomainFullyApproved_ReturnsFalse_WhenOptionalAdminReviewRejected()
    {
        var required = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            IsRequired = true,
            RequiresAdminReview = false
        };
        var optional = new TeacherDomainQuestion
        {
            Id = 2,
            DomainId = DomainId,
            IsRequired = false,
            RequiresAdminReview = true
        };

        var service = BuildCascadeService(
            [required, optional],
            [
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Approved
                },
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 2,
                    VerificationStatus = DocumentVerificationStatus.Rejected
                }
            ]);

        Assert.False(await service.IsDomainFullyApprovedForTeacherAsync(TeacherId, DomainId));
    }

    [Fact]
    public async Task IsDomainFullyApproved_ReturnsTrue_WhenOptionalAdminReviewNotSubmitted()
    {
        var required = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            IsRequired = true,
            RequiresAdminReview = false
        };
        var optional = new TeacherDomainQuestion
        {
            Id = 2,
            DomainId = DomainId,
            IsRequired = false,
            RequiresAdminReview = true
        };

        var service = BuildCascadeService(
            [required, optional],
            [
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Approved
                }
            ]);

        Assert.True(await service.IsDomainFullyApprovedForTeacherAsync(TeacherId, DomainId));
    }

    [Fact]
    public async Task IsDomainFullyApproved_ReturnsTrue_WhenOptionalAdminReviewApproved()
    {
        var required = new TeacherDomainQuestion
        {
            Id = 1,
            DomainId = DomainId,
            IsRequired = true,
            RequiresAdminReview = false
        };
        var optional = new TeacherDomainQuestion
        {
            Id = 2,
            DomainId = DomainId,
            IsRequired = false,
            RequiresAdminReview = true
        };

        var service = BuildCascadeService(
            [required, optional],
            [
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 1,
                    VerificationStatus = DocumentVerificationStatus.Approved
                },
                new TeacherDomainQuestionSubmission
                {
                    TeacherId = TeacherId,
                    QuestionId = 2,
                    VerificationStatus = DocumentVerificationStatus.Approved
                }
            ]);

        Assert.True(await service.IsDomainFullyApprovedForTeacherAsync(TeacherId, DomainId));
    }

    private static TeacherDomainSubjectCascadeService BuildCascadeService(
        List<TeacherDomainQuestion> questions,
        List<TeacherDomainQuestionSubmission> submissions)
    {
        var questionRepo = new Mock<ITeacherDomainQuestionRepository>();
        questionRepo
            .Setup(r => r.GetActiveByDomainIdAsync(DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questions);

        var submissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherAndDomainIdAsync(TeacherId, DomainId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissions);

        return new TeacherDomainSubjectCascadeService(
            Mock.Of<ITeacherSubjectRepository>(),
            questionRepo.Object,
            submissionRepo.Object,
            NullLogger<TeacherDomainSubjectCascadeService>.Instance);
    }
}
