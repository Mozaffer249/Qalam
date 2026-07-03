using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherRegistrationNextStepTests
{
    private const int UserId = 10;
    private const int TeacherId = 20;

    [Fact]
    public async Task GetNextStep_ActiveWithoutSubjects_ReturnsAddSubjects()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.Active,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Add Teaching Subjects and Units", step.NextStepName);
        Assert.False(step.IsRegistrationComplete);
    }

    [Fact]
    public async Task GetNextStep_ActiveWithoutAvailability_ReturnsDashboardWithAvailabilityFlag()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.Active,
            hasAvailability: false,
            hasSubjects: true,
            canActivate: false);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Dashboard", step.NextStepName);
        Assert.True(step.IsRegistrationComplete);
        Assert.True(step.RequiresAvailabilitySetup);
    }

    [Fact]
    public async Task GetNextStep_ActiveWithAvailability_ReturnsDashboardWithoutAvailabilityFlag()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.Active,
            hasAvailability: true,
            hasSubjects: true,
            canActivate: false);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Dashboard", step.NextStepName);
        Assert.True(step.IsRegistrationComplete);
        Assert.False(step.RequiresAvailabilitySetup);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWhenCanActivate_ReturnsAwaitingFinalApproval()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: true,
            canActivate: true);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Awaiting Final Approval", step.NextStepName);
        Assert.True(step.AwaitingFinalApproval);
        Assert.False(step.IsRegistrationComplete);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWhenNotReady_ReturnsAwaitingDomainVerification()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: true,
            canActivate: false,
            hasPendingRegistrationReview: true);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Awaiting Domain Verification", step.NextStepName);
        Assert.False(step.AwaitingFinalApproval);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWithRejectedDomainQuestion_ReturnsFixDomainVerification()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.DomainQuestion,
                DomainId = 1,
                DomainCode = "school",
                Label = "Professional teacher license",
                RejectionReason = "Expired license"
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: true,
            canActivate: false,
            corrections: corrections,
            hasRejectedDomainQuestions: true);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Fix Domain Verification", step.NextStepName);
        Assert.NotNull(step.PendingCorrections);
        Assert.Single(step.PendingCorrections!);
    }

    [Fact]
    public async Task GetNextStep_RejectedAndIncompleteCatalog_ReturnsFixDomainNotAddSubjects()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.DomainQuestion,
                DomainId = 4,
                DomainCode = "skills",
                SubmissionId = 15,
                Label = "Professional or training certification",
                RejectionReason = "Invalid certificate"
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [1, 2, 3, 4],
            hasIncompleteCatalogAnswers: true,
            hasAnyFullyApprovedCatalogDomain: true,
            hasRejectedDomainQuestions: true,
            corrections: corrections);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Fix Domain Verification", step.NextStepName);
        Assert.NotEqual("Add Teaching Subjects and Units", step.NextStepName);
        Assert.NotEqual("Complete Domain Questions", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_ActiveWithRejectedDomain_ReturnsFixDomainNotAddSubjects()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.DomainQuestion,
                DomainId = 4,
                DomainCode = "skills",
                Label = "Skills certification",
                RejectionReason = "Rejected"
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.Active,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            hasRejectedDomainQuestions: true,
            hasIncompleteCatalogAnswers: true,
            corrections: corrections);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Fix Domain Verification", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_RejectedDomainWithEmptyReason_StillReturnsFixDomain()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.DomainQuestion,
                DomainId = 4,
                DomainCode = "skills",
                Label = "Skills certification",
                RejectionReason = string.Empty
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            hasRejectedDomainQuestions: true,
            corrections: corrections);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Fix Domain Verification", step.NextStepName);
        Assert.NotNull(step.PendingCorrections);
        Assert.Single(step.PendingCorrections!);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWithSubjectCorrection_IgnoresSubjectAndContinuesFlow()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.Subject,
                TeacherSubjectId = 99,
                Label = "Math",
                RejectionReason = "Certificate mismatch"
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: true,
            canActivate: false,
            corrections: corrections);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Awaiting Domain Verification", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWithRejectedRegistrationDocument_ReturnsReupload()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.RegistrationDocument,
                DocumentId = 5,
                Label = "Identity",
                RejectionReason = "Blurry image"
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            corrections: corrections);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Re-upload Rejected Documents", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWithIncompleteCatalogDomains_ReturnsCompleteDomainQuestions()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [1, 2],
            hasIncompleteCatalogAnswers: true);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Complete Domain Questions", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWithPendingAnswersAndApprovedDomain_ReturnsAwaitingDomainVerification()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [1, 2],
            hasAnyAnswersPendingAdminReview: true,
            hasAnyFullyApprovedCatalogDomain: true);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Awaiting Domain Verification", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWithApprovedDomainAndNoPendingAnswers_ReturnsAddSubjects()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [1, 2],
            hasAnyAnswersPendingAdminReview: false,
            hasAnyFullyApprovedCatalogDomain: true,
            hasRejectedDomainQuestions: false);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Add Teaching Subjects and Units", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_AfterOneDomainApprovedAndOthersIncomplete_ReturnsAddSubjects()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [1, 2, 3, 4],
            hasIncompleteCatalogAnswers: false,
            hasAnyAnswersPendingAdminReview: false,
            hasAnyFullyApprovedCatalogDomain: true,
            hasRejectedDomainQuestions: false);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Add Teaching Subjects and Units", step.NextStepName);
        Assert.NotEqual("Complete Domain Questions", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_AfterResubmitApproved_NoRejectedLatest_ReturnsAddSubjectsNotFixDomain()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [3, 4],
            hasIncompleteCatalogAnswers: false,
            hasAnyAnswersPendingAdminReview: false,
            hasAnyFullyApprovedCatalogDomain: true,
            hasRejectedDomainQuestions: false);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Add Teaching Subjects and Units", step.NextStepName);
        Assert.NotEqual("Fix Domain Verification", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_DocumentsRejectedWithNoRejectedLatest_ReturnsAddSubjects()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.DocumentsRejected,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [3, 4],
            hasIncompleteCatalogAnswers: false,
            hasAnyAnswersPendingAdminReview: false,
            hasAnyFullyApprovedCatalogDomain: true,
            hasRejectedDomainQuestions: false);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Add Teaching Subjects and Units", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_ReturnsFixDomain_WhenRejectedAndApprovedDomainCoexist()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.DomainQuestion,
                DomainId = 2,
                DomainCode = "skills",
                Label = "Skills certification",
                RejectionReason = "Invalid certificate"
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [1, 2],
            hasAnyAnswersPendingAdminReview: false,
            hasAnyFullyApprovedCatalogDomain: true,
            hasRejectedDomainQuestions: true,
            corrections: corrections);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Fix Domain Verification", step.NextStepName);
        Assert.NotNull(step.PendingCorrections);
        Assert.Single(step.PendingCorrections!);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWithPendingDomainReview_ReturnsAwaitingDomainVerification()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            catalogDomainIds: [1, 2],
            hasCatalogDomainsPendingReview: true);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Awaiting Domain Verification", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_PendingVerificationWhenAllCatalogDomainsApproved_ReturnsAwaitingFinalApproval()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: true,
            catalogDomainIds: [1, 2],
            allCatalogDomainsApproved: true);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Awaiting Final Approval", step.NextStepName);
        Assert.True(step.AwaitingFinalApproval);
    }

    [Fact]
    public async Task GetNextStep_DocumentsRejectedWithDomainReject_ReturnsFixDomainVerification()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.DomainQuestion,
                DomainId = 1,
                DomainCode = "school",
                Label = "License",
                RejectionReason = "Expired"
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.DocumentsRejected,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            corrections: corrections,
            hasRejectedDomainQuestions: true);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Fix Domain Verification", step.NextStepName);
    }

    [Fact]
    public async Task GetNextStep_DocumentsRejectedWithRegistrationReject_ReturnsReupload()
    {
        var corrections = new List<TeacherReviewCorrectionDto>
        {
            new()
            {
                Type = TeacherReviewCorrectionType.RegistrationDocument,
                DocumentId = 5,
                Label = "Identity",
                RejectionReason = "Blurry image"
            },
            new()
            {
                Type = TeacherReviewCorrectionType.DomainQuestion,
                DomainId = 1,
                DomainCode = "school",
                Label = "License",
                RejectionReason = "Expired"
            }
        };

        var service = BuildService(
            teacherStatus: TeacherStatus.DocumentsRejected,
            hasAvailability: false,
            hasSubjects: false,
            canActivate: false,
            corrections: corrections);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Re-upload Rejected Documents", step.NextStepName);
    }

    private static TeacherRegistrationService BuildService(
        TeacherStatus teacherStatus,
        bool hasAvailability,
        bool hasSubjects,
        bool canActivate,
        List<TeacherReviewCorrectionDto>? corrections = null,
        List<int>? catalogDomainIds = null,
        bool hasIncompleteCatalogAnswers = false,
        bool hasCatalogDomainsPendingReview = false,
        bool allCatalogDomainsApproved = false,
        bool hasPendingRegistrationReview = false,
        bool hasAnyAnswersPendingAdminReview = false,
        bool hasAnyFullyApprovedCatalogDomain = false,
        bool hasRejectedDomainQuestions = false)
    {
        var user = new User { Id = UserId, FirstName = "Test", LastName = "Teacher" };
        var userStore = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            userStore.Object, null, null, null, null, null, null, null, null);
        userManager.Setup(m => m.FindByIdAsync(UserId.ToString())).ReturnsAsync(user);

        var teacher = new Teacher { Id = TeacherId, UserId = UserId, Status = teacherStatus };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(teacher);

        var documentRepo = new Mock<ITeacherDocumentRepository>();
        documentRepo.Setup(r => r.GetRejectedDocumentsAsync(TeacherId)).ReturnsAsync([]);

        var subjectRepo = new Mock<ITeacherSubjectRepository>();
        subjectRepo.Setup(r => r.HasAnySubjectOfferingsAsync(TeacherId)).ReturnsAsync(hasSubjects);

        var availabilityRepo = new Mock<ITeacherAvailabilityRepository>();
        availabilityRepo.Setup(r => r.HasAnyAvailabilityAsync(TeacherId)).ReturnsAsync(hasAvailability);

        var completionService = new Mock<ITeacherRegistrationCompletionService>();
        completionService
            .Setup(s => s.CanActivateTeacherAccountAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canActivate);
        completionService
            .Setup(s => s.HasPendingRequiredRegistrationReviewAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasPendingRegistrationReview);

        var reviewCorrectionService = new Mock<ITeacherReviewCorrectionService>();
        reviewCorrectionService
            .Setup(s => s.GetPendingCorrectionsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(corrections ?? []);

        var domainQuestionStatusService = new Mock<ITeacherDomainQuestionStatusService>();
        var catalogIds = catalogDomainIds ?? [];
        domainQuestionStatusService
            .Setup(s => s.GetCatalogDomainIdsWithRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalogIds);
        domainQuestionStatusService
            .Setup(s => s.HasIncompleteCatalogDomainAnswersAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasIncompleteCatalogAnswers);
        domainQuestionStatusService
            .Setup(s => s.HasCatalogDomainsPendingAdminReviewAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasCatalogDomainsPendingReview);
        domainQuestionStatusService
            .Setup(s => s.AreAllCatalogDomainsFullyApprovedAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allCatalogDomainsApproved);
        domainQuestionStatusService
            .Setup(s => s.HasAnyAnswersPendingAdminReviewAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasAnyAnswersPendingAdminReview);
        domainQuestionStatusService
            .Setup(s => s.HasAnyFullyApprovedCatalogDomainAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasAnyFullyApprovedCatalogDomain);
        domainQuestionStatusService
            .Setup(s => s.HasRejectedDomainQuestionsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasRejectedDomainQuestions);
        var rejectedDomainCorrections = (corrections ?? [])
            .Where(c => c.Type == TeacherReviewCorrectionType.DomainQuestion)
            .ToList();
        domainQuestionStatusService
            .Setup(s => s.GetRejectedDomainCorrectionsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasRejectedDomainQuestions ? rejectedDomainCorrections : []);

        return new TeacherRegistrationService(
            userManager.Object,
            teacherRepo.Object,
            documentRepo.Object,
            Mock.Of<IAuthenticationService>(),
            NullLogger<TeacherRegistrationService>.Instance,
            subjectRepo.Object,
            availabilityRepo.Object,
            Mock.Of<IAuthLoginOtpHelper>(),
            Mock.Of<ITeacherLifecycleEmailService>(),
            completionService.Object,
            reviewCorrectionService.Object,
            domainQuestionStatusService.Object);
    }
}

public class TeacherRegistrationStatusServiceTests
{
    private const int TeacherId = 55;

    [Fact]
    public async Task GetStatus_AwaitingFinalApproval_WhenCanActivateAndNotActive()
    {
        var service = BuildStatusService(
            TeacherStatus.PendingVerification,
            canActivate: true,
            hasAvailability: false,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 2, Approved = 2 });

        var status = await service.GetStatusForTeacherAsync(TeacherId);

        Assert.Equal(TeacherStatus.PendingVerification, status.TeacherStatus);
        Assert.False(status.IsAccountActivated);
        Assert.True(status.CanBeActivated);
        Assert.True(status.AwaitingFinalApproval);
        Assert.False(status.RequiresAvailabilitySetup);
        Assert.Equal(2, status.SubjectSummary.TotalSubjects);
    }

    [Fact]
    public async Task GetStatus_ActivatedWithoutAvailability_RequiresAvailabilitySetup()
    {
        var service = BuildStatusService(
            TeacherStatus.Active,
            canActivate: false,
            hasAvailability: false,
            snapshot: new TeacherSubjectActivationSnapshot { Total = 1, Approved = 1 });

        var status = await service.GetStatusForTeacherAsync(TeacherId);

        Assert.True(status.IsAccountActivated);
        Assert.False(status.AwaitingFinalApproval);
        Assert.True(status.RequiresAvailabilitySetup);
    }

    private static TeacherRegistrationStatusService BuildStatusService(
        TeacherStatus status,
        bool canActivate,
        bool hasAvailability,
        TeacherSubjectActivationSnapshot snapshot)
    {
        var teacher = new Teacher { Id = TeacherId, Status = status };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(TeacherId)).ReturnsAsync(teacher);

        var requirementRepo = new Mock<ITeacherRegistrationRequirementRepository>();
        requirementRepo
            .Setup(r => r.GetActiveOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var submissionRepo = new Mock<ITeacherRegistrationSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdWithRequirementsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var documentRepo = new Mock<ITeacherDocumentRepository>();
        documentRepo.Setup(r => r.GetDocumentsStatusAsync(TeacherId)).ReturnsAsync([]);

        var completionService = new Mock<ITeacherRegistrationCompletionService>();
        completionService
            .Setup(s => s.CanActivateTeacherAccountAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canActivate);

        var availabilityRepo = new Mock<ITeacherAvailabilityRepository>();
        availabilityRepo.Setup(r => r.HasAnyAvailabilityAsync(TeacherId)).ReturnsAsync(hasAvailability);

        var subjectRepo = new Mock<ITeacherSubjectRepository>();
        subjectRepo.Setup(r => r.GetSubjectActivationSnapshotAsync(TeacherId)).ReturnsAsync(snapshot);

        var registrationService = new Mock<ITeacherRegistrationService>();

        return new TeacherRegistrationStatusService(
            requirementRepo.Object,
            submissionRepo.Object,
            documentRepo.Object,
            teacherRepo.Object,
            completionService.Object,
            availabilityRepo.Object,
            subjectRepo.Object,
            registrationService.Object);
    }
}

public class TeacherAccountStatusServiceTests
{
    private const int UserId = 10;
    private const int TeacherId = 20;

    [Fact]
    public async Task GetAccountStatus_AwaitingFinalApproval_IncludesMatchingNextStep()
    {
        var nextStep = new RegistrationStepDto
        {
            NextStepName = "Awaiting Final Approval",
            AwaitingFinalApproval = true,
            IsRegistrationComplete = false
        };

        var service = BuildAccountStatusService(
            TeacherStatus.PendingVerification,
            canActivate: true,
            hasAvailability: false,
            nextStep);

        var status = await service.GetAccountStatusForTeacherAsync(TeacherId, UserId);

        Assert.Equal(TeacherStatus.PendingVerification, status.TeacherStatus);
        Assert.False(status.IsAccountActivated);
        Assert.True(status.CanBeActivated);
        Assert.True(status.AwaitingFinalApproval);
        Assert.False(status.RequiresAvailabilitySetup);
        Assert.Equal("Awaiting Final Approval", status.NextStep.NextStepName);
        Assert.True(status.NextStep.AwaitingFinalApproval);
    }

    [Fact]
    public async Task GetAccountStatus_ActiveWithoutAvailability_RequiresAvailabilitySetup()
    {
        var nextStep = new RegistrationStepDto
        {
            NextStepName = "Dashboard",
            IsRegistrationComplete = true,
            RequiresAvailabilitySetup = true
        };

        var service = BuildAccountStatusService(
            TeacherStatus.Active,
            canActivate: false,
            hasAvailability: false,
            nextStep);

        var status = await service.GetAccountStatusForTeacherAsync(TeacherId, UserId);

        Assert.True(status.IsAccountActivated);
        Assert.False(status.AwaitingFinalApproval);
        Assert.True(status.RequiresAvailabilitySetup);
        Assert.Equal("Dashboard", status.NextStep.NextStepName);
        Assert.True(status.NextStep.RequiresAvailabilitySetup);
    }

    [Fact]
    public async Task GetAccountStatus_ActiveWithAvailability_ReturnsDashboardWithoutFlags()
    {
        var nextStep = new RegistrationStepDto
        {
            NextStepName = "Dashboard",
            IsRegistrationComplete = true,
            RequiresAvailabilitySetup = false
        };

        var service = BuildAccountStatusService(
            TeacherStatus.Active,
            canActivate: false,
            hasAvailability: true,
            nextStep);

        var status = await service.GetAccountStatusForTeacherAsync(TeacherId, UserId);

        Assert.True(status.IsAccountActivated);
        Assert.False(status.AwaitingFinalApproval);
        Assert.False(status.RequiresAvailabilitySetup);
        Assert.Equal("Dashboard", status.NextStep.NextStepName);
        Assert.False(status.NextStep.RequiresAvailabilitySetup);
    }

    [Fact]
    public async Task GetAccountStatus_DoesNotThrow_WithMultipleCertificateSubmissions()
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

        var nextStep = new RegistrationStepDto
        {
            NextStepName = "Add Teaching Subjects and Units",
            IsRegistrationComplete = false
        };

        var service = BuildAccountStatusServiceWithCompletion(
            TeacherStatus.PendingVerification,
            hasAvailability: false,
            nextStep,
            [identityRequirement, certificateRequirement],
            submissions);

        var status = await service.GetAccountStatusForTeacherAsync(TeacherId, UserId);

        Assert.Equal(TeacherStatus.PendingVerification, status.TeacherStatus);
        Assert.Equal("Add Teaching Subjects and Units", status.NextStep.NextStepName);
    }

    private static TeacherRegistrationStatusService BuildAccountStatusService(
        TeacherStatus status,
        bool canActivate,
        bool hasAvailability,
        RegistrationStepDto nextStep)
    {
        var teacher = new Teacher { Id = TeacherId, UserId = UserId, Status = status };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(TeacherId)).ReturnsAsync(teacher);

        var registrationService = new Mock<ITeacherRegistrationService>();
        registrationService
            .Setup(s => s.GetNextRegistrationStepAsync(UserId))
            .ReturnsAsync(nextStep);

        var completionService = new Mock<ITeacherRegistrationCompletionService>();
        completionService
            .Setup(s => s.CanActivateTeacherAccountAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canActivate);

        var availabilityRepo = new Mock<ITeacherAvailabilityRepository>();
        availabilityRepo.Setup(r => r.HasAnyAvailabilityAsync(TeacherId)).ReturnsAsync(hasAvailability);

        return new TeacherRegistrationStatusService(
            Mock.Of<ITeacherRegistrationRequirementRepository>(),
            Mock.Of<ITeacherRegistrationSubmissionRepository>(),
            Mock.Of<ITeacherDocumentRepository>(),
            teacherRepo.Object,
            completionService.Object,
            availabilityRepo.Object,
            Mock.Of<ITeacherSubjectRepository>(),
            registrationService.Object);
    }

    private static TeacherRegistrationStatusService BuildAccountStatusServiceWithCompletion(
        TeacherStatus status,
        bool hasAvailability,
        RegistrationStepDto nextStep,
        List<TeacherRegistrationRequirement> requirements,
        List<TeacherRegistrationSubmission> submissions)
    {
        var teacher = new Teacher { Id = TeacherId, UserId = UserId, Status = status };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(TeacherId)).ReturnsAsync(teacher);

        var requirementRepo = new Mock<ITeacherRegistrationRequirementRepository>();
        requirementRepo
            .Setup(r => r.GetActiveOrderedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(requirements);

        var submissionRepo = new Mock<ITeacherRegistrationSubmissionRepository>();
        submissionRepo
            .Setup(r => r.GetByTeacherIdWithRequirementsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissions);

        var documentRepo = new Mock<ITeacherDocumentRepository>();
        documentRepo.Setup(r => r.GetByTeacherIdAsync(TeacherId)).ReturnsAsync([]);

        var domainQuestionRepo = new Mock<ITeacherDomainQuestionRepository>();
        domainQuestionRepo
            .Setup(r => r.GetDomainIdsWithActiveRequiredQuestionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        domainQuestionRepo
            .Setup(r => r.GetActiveByDomainIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var domainSubmissionRepo = new Mock<ITeacherDomainQuestionSubmissionRepository>();
        domainSubmissionRepo
            .Setup(r => r.GetByTeacherIdAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        domainSubmissionRepo
            .Setup(r => r.GetByTeacherIdWithQuestionsAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var completionService = new TeacherRegistrationCompletionService(
            requirementRepo.Object,
            submissionRepo.Object,
            documentRepo.Object,
            teacherRepo.Object,
            domainQuestionRepo.Object,
            domainSubmissionRepo.Object,
            Mock.Of<ITeacherLifecycleEmailService>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TeacherRegistrationCompletionService>.Instance);

        var registrationService = new Mock<ITeacherRegistrationService>();
        registrationService
            .Setup(s => s.GetNextRegistrationStepAsync(UserId))
            .ReturnsAsync(nextStep);

        var availabilityRepo = new Mock<ITeacherAvailabilityRepository>();
        availabilityRepo.Setup(r => r.HasAnyAvailabilityAsync(TeacherId)).ReturnsAsync(hasAvailability);

        return new TeacherRegistrationStatusService(
            requirementRepo.Object,
            submissionRepo.Object,
            documentRepo.Object,
            teacherRepo.Object,
            completionService,
            availabilityRepo.Object,
            Mock.Of<ITeacherSubjectRepository>(),
            registrationService.Object);
    }
}
