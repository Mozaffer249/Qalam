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
    public async Task GetNextStep_PendingVerificationWhenNotReady_ReturnsAwaitingAdminVerification()
    {
        var service = BuildService(
            teacherStatus: TeacherStatus.PendingVerification,
            hasAvailability: false,
            hasSubjects: true,
            canActivate: false);

        var step = await service.GetNextRegistrationStepAsync(UserId);

        Assert.Equal("Awaiting Admin Verification", step.NextStepName);
        Assert.False(step.AwaitingFinalApproval);
    }

    private static TeacherRegistrationService BuildService(
        TeacherStatus teacherStatus,
        bool hasAvailability,
        bool hasSubjects,
        bool canActivate)
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
            completionService.Object);
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
}
