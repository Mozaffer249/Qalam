using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherRegistrationCompletionServiceTests
{
    private const int TeacherId = 42;
    private const int AdminId = 1;

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenNoSubjects()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 });

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenSubjectPendingButDomainApproved()
    {
        var domainQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            IsRequired = true,
            RequiresAdminReview = true
        };

        var domainSubmission = new TeacherDomainQuestionSubmission
        {
            Id = 100,
            TeacherId = TeacherId,
            QuestionId = domainQuestion.Id,
            Question = domainQuestion,
            VerificationStatus = DocumentVerificationStatus.Approved
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Inactive = 1 },
            domainIds: [1],
            domainQuestions: [domainQuestion],
            domainSubmissions: [domainSubmission],
            hasApprovedDomain: true);

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenDocsAndSubjectsApproved()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 2, Active = 2 });

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task Activate_Succeeds_WhenReady()
    {
        TeacherStatus? updatedStatus = null;
        var lifecycleEmail = new Mock<ITeacherLifecycleEmailService>();
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Active = 1 },
            onStatusUpdate: status => updatedStatus = status,
            lifecycleEmail: lifecycleEmail.Object);

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(TeacherStatus.Active, updatedStatus);
        lifecycleEmail.Verify(
            e => e.SendAccountActivatedAsync(TeacherId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Activate_Fails_WhenAlreadyActive()
    {
        var lifecycleEmail = new Mock<ITeacherLifecycleEmailService>();
        var service = BuildService(
            teacherStatus: TeacherStatus.Active,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Active = 1 },
            lifecycleEmail: lifecycleEmail.Object);

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.False(success);
        Assert.Contains("already active", error, StringComparison.OrdinalIgnoreCase);
        lifecycleEmail.Verify(
            e => e.SendAccountActivatedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Activate_Fails_WhenBlocked()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.Blocked,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Active = 1 });

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.False(success);
        Assert.Contains("blocked", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Activate_Fails_WhenDocumentStillPending()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: false,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Active = 1 });

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.False(success);
        Assert.Contains("pending", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshTeacherStatus_DoesNotAutoActivate_WhenAllApproved()
    {
        TeacherStatus? updatedStatus = null;
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Active = 1 },
            onStatusUpdate: status => updatedStatus = status);

        await service.RefreshTeacherStatusAfterReviewAsync(TeacherId);

        Assert.Equal(TeacherStatus.PendingVerification, updatedStatus);
    }

    [Fact]
    public async Task CanActivate_ReturnsFalse_WhenDomainQuestionRequiresReviewAndPending()
    {
        var domainQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            NameAr = "خبرة",
            NameEn = "Experience",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = true
        };

        var domainSubmission = new TeacherDomainQuestionSubmission
        {
            Id = 100,
            TeacherId = TeacherId,
            QuestionId = domainQuestion.Id,
            Question = domainQuestion,
            VerificationStatus = DocumentVerificationStatus.Pending,
            TextValue = "5 years"
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Active = 1 },
            domainIds: [1],
            domainQuestions: [domainQuestion],
            domainSubmissions: [domainSubmission]);

        Assert.False(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenDomainQuestionAutoApproved()
    {
        var domainQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            NameAr = "خبرة",
            NameEn = "Experience",
            RequirementType = RegistrationRequirementType.Text,
            IsActive = true,
            IsRequired = true,
            RequiresAdminReview = false
        };

        var domainSubmission = new TeacherDomainQuestionSubmission
        {
            Id = 100,
            TeacherId = TeacherId,
            QuestionId = domainQuestion.Id,
            Question = domainQuestion,
            VerificationStatus = DocumentVerificationStatus.Approved,
            TextValue = "5 years"
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Active = 1 },
            domainIds: [1],
            domainQuestions: [domainQuestion],
            domainSubmissions: [domainSubmission],
            hasApprovedDomain: true);

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenOneOfTwoDomainsFullyApproved()
    {
        const int domain2 = 2;
        var schoolQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            IsRequired = true,
            RequiresAdminReview = true
        };
        var quranQuestion = new TeacherDomainQuestion
        {
            Id = 11,
            DomainId = domain2,
            Code = "quran_ijaza",
            IsRequired = true,
            RequiresAdminReview = true
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, domain2],
            domainQuestions: [schoolQuestion, quranQuestion],
            domainSubmissions:
            [
                new TeacherDomainQuestionSubmission
                {
                    Id = 100,
                    TeacherId = TeacherId,
                    QuestionId = schoolQuestion.Id,
                    Question = schoolQuestion,
                    VerificationStatus = DocumentVerificationStatus.Approved
                }
            ],
            hasApprovedDomain: true);

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task CanActivate_ReturnsFalse_WhenNoDomainApprovalExists()
    {
        var schoolQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            IsRequired = true,
            RequiresAdminReview = true
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1],
            domainQuestions: [schoolQuestion],
            domainSubmissions:
            [
                new TeacherDomainQuestionSubmission
                {
                    Id = 100,
                    TeacherId = TeacherId,
                    QuestionId = schoolQuestion.Id,
                    Question = schoolQuestion,
                    VerificationStatus = DocumentVerificationStatus.Approved
                }
            ],
            hasApprovedDomain: false);

        Assert.False(await service.CanActivateTeacherAccountAsync(TeacherId));
        var reasons = await service.GetActivationBlockReasonsAsync(TeacherId);
        Assert.Contains(reasons, r => r.Contains("approved by an admin", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CanActivate_ReturnsTrue_WhenOneDomainApproved_EvenIfOtherDomainRejected()
    {
        const int domain2 = 2;
        var schoolQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            IsRequired = true,
            RequiresAdminReview = true
        };
        var licenseQuestion = new TeacherDomainQuestion
        {
            Id = 11,
            DomainId = domain2,
            Code = "quran_license",
            IsRequired = true,
            RequiresAdminReview = true
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, domain2],
            domainQuestions: [schoolQuestion, licenseQuestion],
            domainSubmissions:
            [
                new TeacherDomainQuestionSubmission
                {
                    Id = 100,
                    TeacherId = TeacherId,
                    QuestionId = schoolQuestion.Id,
                    Question = schoolQuestion,
                    VerificationStatus = DocumentVerificationStatus.Approved
                },
                new TeacherDomainQuestionSubmission
                {
                    Id = 101,
                    TeacherId = TeacherId,
                    QuestionId = licenseQuestion.Id,
                    Question = licenseQuestion,
                    VerificationStatus = DocumentVerificationStatus.Rejected,
                    RejectionReason = "Expired"
                }
            ],
            hasApprovedDomain: true);

        Assert.True(await service.CanActivateTeacherAccountAsync(TeacherId));
    }

    [Fact]
    public async Task Activate_Succeeds_WhenDocumentsRejectedStatus_ButReadinessClear()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.DocumentsRejected,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Active = 1 });

        var (success, error) = await service.ActivateTeacherAccountAsync(TeacherId, AdminId);

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task RefreshTeacherStatus_DoesNotDowngrade_WhenRejectedInOtherDomain_AndOneDomainApproved()
    {
        const int domain2 = 2;
        var schoolQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "school_experience",
            IsRequired = true,
            RequiresAdminReview = true
        };
        var quranQuestion = new TeacherDomainQuestion
        {
            Id = 11,
            DomainId = domain2,
            Code = "quran_ijaza",
            IsRequired = true,
            RequiresAdminReview = true
        };

        TeacherStatus? updatedStatus = null;
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            onStatusUpdate: status => updatedStatus = status,
            domainIds: [1, domain2],
            domainQuestions: [schoolQuestion, quranQuestion],
            domainSubmissions:
            [
                new TeacherDomainQuestionSubmission
                {
                    Id = 100,
                    TeacherId = TeacherId,
                    QuestionId = schoolQuestion.Id,
                    Question = schoolQuestion,
                    VerificationStatus = DocumentVerificationStatus.Approved
                },
                new TeacherDomainQuestionSubmission
                {
                    Id = 101,
                    TeacherId = TeacherId,
                    QuestionId = quranQuestion.Id,
                    Question = quranQuestion,
                    VerificationStatus = DocumentVerificationStatus.Rejected,
                    RejectionReason = "Invalid"
                }
            ],
            hasApprovedDomain: true);

        await service.RefreshTeacherStatusAfterReviewAsync(TeacherId);

        Assert.Equal(TeacherStatus.PendingVerification, updatedStatus);
    }

    [Fact]
    public async Task CanActivate_DoesNotThrow_WithMultipleCertificateSubmissions()
    {
        var identityRequirement = new TeacherRegistrationRequirement
        {
            Id = 1,
            Code = "identity",
            NameAr = "identity",
            NameEn = "identity",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 1
        };

        var certificateRequirement = new TeacherRegistrationRequirement
        {
            Id = 2,
            Code = "certificate",
            NameAr = "certificate",
            NameEn = "certificate",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 5
        };

        var submissions = new List<TeacherRegistrationSubmission>
        {
            new()
            {
                Id = 1,
                TeacherId = TeacherId,
                RequirementId = identityRequirement.Id,
                Requirement = identityRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            },
            new()
            {
                Id = 2,
                TeacherId = TeacherId,
                RequirementId = certificateRequirement.Id,
                Requirement = certificateRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            },
            new()
            {
                Id = 3,
                TeacherId = TeacherId,
                RequirementId = certificateRequirement.Id,
                Requirement = certificateRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            requirements: [identityRequirement, certificateRequirement],
            registrationSubmissions: submissions);

        var canActivate = await service.CanActivateTeacherAccountAsync(TeacherId);

        Assert.True(canActivate);
    }

    [Fact]
    public async Task RefreshTeacherStatusAfterReview_SetsDocumentsRejected_WhenDomainRejectedAndMultiFileCerts()
    {
        TeacherStatus? updatedStatus = null;

        var identityRequirement = new TeacherRegistrationRequirement
        {
            Id = 1,
            Code = "identity",
            NameAr = "identity",
            NameEn = "identity",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 1
        };

        var certificateRequirement = new TeacherRegistrationRequirement
        {
            Id = 2,
            Code = "certificate",
            NameAr = "certificate",
            NameEn = "certificate",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 5
        };

        var domainQuestion = new TeacherDomainQuestion
        {
            Id = 10,
            DomainId = 1,
            Code = "skills_certification",
            IsRequired = false,
            RequiresAdminReview = true
        };

        var submissions = new List<TeacherRegistrationSubmission>
        {
            new()
            {
                Id = 1,
                TeacherId = TeacherId,
                RequirementId = identityRequirement.Id,
                Requirement = identityRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            },
            new()
            {
                Id = 2,
                TeacherId = TeacherId,
                RequirementId = certificateRequirement.Id,
                Requirement = certificateRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            },
            new()
            {
                Id = 3,
                TeacherId = TeacherId,
                RequirementId = certificateRequirement.Id,
                Requirement = certificateRequirement,
                VerificationStatus = DocumentVerificationStatus.Approved
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            requirements: [identityRequirement, certificateRequirement],
            registrationSubmissions: submissions,
            domainIds: [1],
            domainQuestions: [domainQuestion],
            domainSubmissions:
            [
                new TeacherDomainQuestionSubmission
                {
                    Id = 100,
                    TeacherId = TeacherId,
                    QuestionId = domainQuestion.Id,
                    Question = domainQuestion,
                    VerificationStatus = DocumentVerificationStatus.Rejected,
                    RejectionReason = "Invalid certificate"
                }
            ],
            onStatusUpdate: status => updatedStatus = status);

        await service.RefreshTeacherStatusAfterReviewAsync(TeacherId);

        Assert.Equal(TeacherStatus.DocumentsRejected, updatedStatus);
    }

    [Fact]
    public async Task HasPartialDomainQuestionReviewOutcome_ReturnsTrue_WhenApprovedAndRejectedDomains()
    {
        var groups = PartialDomainGroups(approvedDomainId: 1, rejectedDomainId: 2);
        var service = BuildServiceWithDomainGroups(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, 2],
            domainGroups: groups,
            hasApprovedDomain: true);

        Assert.True(await service.HasPartialDomainQuestionReviewOutcomeAsync(TeacherId));
    }

    [Fact]
    public async Task HasPartialDomainQuestionReviewOutcome_ReturnsFalse_WhenAllDomainsApprovedOnly()
    {
        var groups = new List<TeacherDomainQuestionGroupDto>
        {
            new()
            {
                DomainId = 1,
                DomainCode = "school",
                DomainNameEn = "School",
                IsApproved = true,
                Questions = [],
            },
            new()
            {
                DomainId = 2,
                DomainCode = "quran",
                DomainNameEn = "Quran",
                IsApproved = true,
                Questions = [],
            },
        };

        var service = BuildServiceWithDomainGroups(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, 2],
            domainGroups: groups,
            hasApprovedDomain: true);

        Assert.False(await service.HasPartialDomainQuestionReviewOutcomeAsync(TeacherId));
    }

    [Fact]
    public async Task HasPartialDomainQuestionReviewOutcome_ReturnsFalse_WhenAllDomainsRejectedOnly()
    {
        var groups = new List<TeacherDomainQuestionGroupDto>
        {
            new()
            {
                DomainId = 1,
                DomainCode = "school",
                DomainNameEn = "School",
                IsApproved = false,
                Questions =
                [
                    new TeacherDomainQuestionSubmissionStatusDto
                    {
                        VerificationStatus = DocumentVerificationStatus.Rejected,
                    },
                ],
            },
        };

        var service = BuildServiceWithDomainGroups(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1],
            domainGroups: groups,
            hasApprovedDomain: false);

        Assert.False(await service.HasPartialDomainQuestionReviewOutcomeAsync(TeacherId));
    }

    [Fact]
    public async Task GetPartialDomainActivationCandidates_ExcludesWhenRegistrationStillPending()
    {
        var groups = PartialDomainGroups(approvedDomainId: 1, rejectedDomainId: 2);
        var service = BuildServiceWithDomainGroups(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: false,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, 2],
            domainGroups: groups,
            hasApprovedDomain: true,
            pendingSummaries:
            [
                new PendingVerificationTeacherSummaryDto { TeacherId = TeacherId, FullName = "Pending Registration" },
            ]);

        var candidates = await service.GetPartialDomainActivationCandidatesAsync();

        Assert.Empty(candidates);
        Assert.False(await service.AreRegistrationRequirementsApprovedForActivationAsync(TeacherId));
        Assert.True(await service.HasPartialDomainQuestionReviewOutcomeAsync(TeacherId));
    }

    [Fact]
    public async Task GetPartialDomainActivationCandidates_IncludesEligibleTeacherOnly()
    {
        const int eligibleId = 42;
        const int ineligibleId = 43;

        var groups = PartialDomainGroups(approvedDomainId: 1, rejectedDomainId: 2);
        var service = BuildServiceWithDomainGroups(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, 2],
            domainGroups: groups,
            hasApprovedDomain: true,
            pendingSummaries:
            [
                new PendingVerificationTeacherSummaryDto { TeacherId = eligibleId, FullName = "Eligible Teacher" },
                new PendingVerificationTeacherSummaryDto { TeacherId = ineligibleId, FullName = "Ineligible Teacher" },
            ],
            teacherId: eligibleId);

        var candidates = await service.GetPartialDomainActivationCandidatesAsync();

        Assert.Single(candidates);
        Assert.Equal(eligibleId, candidates[0].TeacherId);
        Assert.Equal(1, candidates[0].ApprovedDomainCount);
        Assert.Equal(1, candidates[0].RejectedDomainCount);
    }

    [Fact]
    public async Task GetPartialDomainActivationCandidates_ExcludesWhenCannotActivate()
    {
        var groups = PartialDomainGroups(approvedDomainId: 1, rejectedDomainId: 2);
        var service = BuildServiceWithDomainGroups(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: false,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, 2],
            domainGroups: groups,
            hasApprovedDomain: true,
            pendingSummaries:
            [
                new PendingVerificationTeacherSummaryDto { TeacherId = TeacherId, FullName = "Blocked Teacher" },
            ]);

        var candidates = await service.GetPartialDomainActivationCandidatesAsync();

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task BulkActivatePartialDomainTeachers_ActivatesEligibleTeachers()
    {
        TeacherStatus? updatedStatus = null;
        var lifecycleEmail = new Mock<ITeacherLifecycleEmailService>();
        var groups = PartialDomainGroups(approvedDomainId: 1, rejectedDomainId: 2);
        var service = BuildServiceWithDomainGroups(
            teacherStatus: TeacherStatus.PendingVerification,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, 2],
            domainGroups: groups,
            hasApprovedDomain: true,
            pendingSummaries:
            [
                new PendingVerificationTeacherSummaryDto { TeacherId = TeacherId, FullName = "Eligible Teacher" },
            ],
            onStatusUpdate: status => updatedStatus = status,
            lifecycleEmail: lifecycleEmail.Object);

        var result = await service.BulkActivatePartialDomainTeachersAsync(AdminId);

        Assert.Equal(1, result.ActivatedCount);
        Assert.Empty(result.Failures);
        Assert.Equal(TeacherStatus.Active, updatedStatus);
        lifecycleEmail.Verify(
            e => e.SendAccountActivatedAsync(TeacherId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BulkActivatePartialDomainTeachers_SkipsAlreadyActiveTeacher()
    {
        var groups = PartialDomainGroups(approvedDomainId: 1, rejectedDomainId: 2);
        var service = BuildServiceWithDomainGroups(
            teacherStatus: TeacherStatus.Active,
            requirementsApproved: true,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 0 },
            domainIds: [1, 2],
            domainGroups: groups,
            hasApprovedDomain: true,
            pendingSummaries:
            [
                new PendingVerificationTeacherSummaryDto { TeacherId = TeacherId, FullName = "Already Active" },
            ]);

        var result = await service.BulkActivatePartialDomainTeachersAsync(AdminId);

        Assert.Equal(0, result.ActivatedCount);
        Assert.Empty(result.Failures);
    }

    private static List<TeacherDomainQuestionGroupDto> PartialDomainGroups(int approvedDomainId, int rejectedDomainId) =>
    [
        new()
        {
            DomainId = approvedDomainId,
            DomainCode = "school",
            DomainNameEn = "School",
            IsApproved = true,
            Questions = [],
        },
        new()
        {
            DomainId = rejectedDomainId,
            DomainCode = "quran",
            DomainNameEn = "Quran",
            IsApproved = false,
            Questions =
            [
                new TeacherDomainQuestionSubmissionStatusDto
                {
                    VerificationStatus = DocumentVerificationStatus.Rejected,
                },
            ],
        },
    ];

    private static TeacherRegistrationCompletionService BuildServiceWithDomainGroups(
        TeacherStatus teacherStatus,
        bool requirementsApproved,
        TeacherSubjectActivationSnapshot snapshot,
        List<int>? domainIds = null,
        List<TeacherDomainQuestionGroupDto>? domainGroups = null,
        List<PendingVerificationTeacherSummaryDto>? pendingSummaries = null,
        Action<TeacherStatus>? onStatusUpdate = null,
        ITeacherLifecycleEmailService? lifecycleEmail = null,
        bool hasApprovedDomain = false,
        int teacherId = TeacherId)
    {
        var domainQuestionStatus = new Mock<ITeacherDomainQuestionStatusService>();
        domainQuestionStatus
            .Setup(s => s.GetChecklistForTeacherAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
                id == teacherId ? (domainGroups ?? []) : []);

        return BuildService(
            teacherStatus,
            requirementsApproved,
            snapshot,
            onStatusUpdate,
            lifecycleEmail,
            domainIds,
            hasApprovedDomain: hasApprovedDomain,
            teacherId: teacherId,
            pendingSummaries: pendingSummaries,
            domainQuestionStatus: domainQuestionStatus.Object);
    }

    private static TeacherRegistrationCompletionService BuildService(
        TeacherStatus teacherStatus,
        bool requirementsApproved,
        TeacherSubjectActivationSnapshot snapshot,
        Action<TeacherStatus>? onStatusUpdate = null,
        ITeacherLifecycleEmailService? lifecycleEmail = null,
        List<int>? domainIds = null,
        List<TeacherDomainQuestion>? domainQuestions = null,
        List<TeacherDomainQuestionSubmission>? domainSubmissions = null,
        List<TeacherRegistrationRequirement>? requirements = null,
        List<TeacherRegistrationSubmission>? registrationSubmissions = null,
        bool hasApprovedDomain = false,
        int teacherId = TeacherId,
        List<PendingVerificationTeacherSummaryDto>? pendingSummaries = null,
        ITeacherDomainQuestionStatusService? domainQuestionStatus = null)
    {
        var teacher = new Teacher { Id = teacherId, Status = teacherStatus };

        var requirement = new TeacherRegistrationRequirement
        {
            Id = 1,
            Code = "identity",
            NameAr = "identity",
            NameEn = "identity",
            RequirementType = RegistrationRequirementType.File,
            IsActive = true,
            IsRequired = true,
            MinCount = 1,
            MaxCount = 1
        };

        var submission = new TeacherRegistrationSubmission
        {
            Id = 1,
            TeacherId = TeacherId,
            RequirementId = requirement.Id,
            Requirement = requirement,
            VerificationStatus = requirementsApproved
                ? DocumentVerificationStatus.Approved
                : DocumentVerificationStatus.Pending
        };

        var activeRequirements = requirements ?? [requirement];
        var activeSubmissions = registrationSubmissions ?? [submission];

        var requirementRepo = new Mock<ITeacherRegistrationRequirementRepository>();
        requirementRepo
            .Setup(r => r.GetActiveOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeRequirements);

        var submissionRepo = new Mock<ITeacherRegistrationSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdWithRequirementsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeSubmissions);

        var documentRepo = new Mock<ITeacherDocumentRepository>();
        documentRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId))
            .ReturnsAsync([]);

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(teacher);
        teacherRepo
            .Setup(r => r.UpdateStatusAsync(teacherId, It.IsAny<TeacherStatus>()))
            .Callback<int, TeacherStatus>((_, status) =>
            {
                teacher.Status = status;
                onStatusUpdate?.Invoke(status);
            })
            .Returns(Task.CompletedTask);
        teacherRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        teacherRepo
            .Setup(r => r.GetPendingVerificationTeacherSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingSummaries ?? []);

        var domainQuestionRepo = new Mock<ITeacherDomainQuestionRepository>();
        domainQuestionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainIds ?? []);
        domainQuestionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainQuestions ?? []);

        var domainSubmissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        domainSubmissionRepo
            .Setup(r => r.GetByTeacherIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainSubmissions ?? []);
        domainSubmissionRepo
            .Setup(r => r.GetByTeacherIdWithQuestionsAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainSubmissions ?? []);

        var domainApproval = new Mock<ITeacherDomainApprovalService>();
        domainApproval
            .Setup(s => s.HasAnyApprovedDomainAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasApprovedDomain);

        return new TeacherRegistrationCompletionService(
            requirementRepo.Object,
            submissionRepo.Object,
            documentRepo.Object,
            teacherRepo.Object,
            domainQuestionRepo.Object,
            domainSubmissionRepo.Object,
            domainApproval.Object,
            domainQuestionStatus ?? Mock.Of<ITeacherDomainQuestionStatusService>(),
            lifecycleEmail ?? Mock.Of<ITeacherLifecycleEmailService>(),
            NullLogger<TeacherRegistrationCompletionService>.Instance);
    }
}
