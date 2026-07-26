using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherRegistrationSubmitServiceTests
{
    private const int TeacherId = 9;

    [Fact]
    public async Task SubmitAsync_PreserveMode_DoesNotDeleteExistingSubmissions_AndInsertsMissingOnly()
    {
        var teacherRepo = new Mock<ITeacherRepository>();
        var documentRepo = new Mock<ITeacherDocumentRepository>();
        var submissionRepo = new Mock<ITeacherRegistrationSubmissionRepository>();
        var fileStorage = new Mock<IFileStorageService>();
        var transaction = new Mock<IDbContextTransaction>();

        submissionRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        submissionRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
        submissionRepo.Setup(r => r.AddAsync(It.IsAny<TeacherRegistrationSubmission>()))
            .ReturnsAsync((TeacherRegistrationSubmission s) => s);
        submissionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        teacherRepo.Setup(r => r.UpdateAsync(It.IsAny<Teacher>())).Returns(Task.CompletedTask);
        teacherRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var teacher = new Teacher { Id = TeacherId, Status = TeacherStatus.PendingVerification };
        var identityReq = new TeacherRegistrationRequirement
        {
            Id = 1,
            Code = TeacherRegistrationRequirementCodes.IdentityDocument,
            IsRequired = true,
            IsActive = true,
            RequirementType = RegistrationRequirementType.File,
        };
        var majorReq = new TeacherRegistrationRequirement
        {
            Id = 2,
            Code = "major",
            IsRequired = true,
            IsActive = true,
            RequirementType = RegistrationRequirementType.Text,
        };

        var alreadySubmitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            TeacherRegistrationRequirementCodes.IdentityDocument,
        };

        var input = new TeacherRegistrationSubmissionInput
        {
            Location = TeacherLocation.InsideSaudiArabia,
            NationalityCode = "SA",
            DocumentNumber = null,
            IdentityDocumentFile = null,
            TextValuesByCode = new Dictionary<string, string?>
            {
                ["major"] = "Computer Science",
            },
        };

        var service = new TeacherRegistrationSubmitService(
            teacherRepo.Object,
            documentRepo.Object,
            submissionRepo.Object,
            fileStorage.Object,
            NullLogger<TeacherRegistrationSubmitService>.Instance);

        await service.SubmitAsync(
            teacher,
            input,
            [identityReq, majorReq],
            alreadySubmitted,
            preserveExistingSubmissions: true,
            CancellationToken.None);

        submissionRepo.Verify(
            r => r.DeleteAllForTeacherAsync(TeacherId, It.IsAny<CancellationToken>()),
            Times.Never);
        documentRepo.Verify(
            r => r.DeletePendingForTeacherAsync(TeacherId, It.IsAny<CancellationToken>()),
            Times.Never);

        submissionRepo.Verify(
            r => r.AddAsync(It.Is<TeacherRegistrationSubmission>(s =>
                s.TeacherId == TeacherId
                && s.RequirementId == majorReq.Id
                && s.TextValue == "Computer Science"
                && s.VerificationStatus == DocumentVerificationStatus.Approved)),
            Times.Once);

        // Identity was already submitted — must not insert another identity submission row.
        submissionRepo.Verify(
            r => r.AddAsync(It.Is<TeacherRegistrationSubmission>(s =>
                s.RequirementId == identityReq.Id)),
            Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_FreshMode_DeletesAllThenInserts()
    {
        var teacherRepo = new Mock<ITeacherRepository>();
        var documentRepo = new Mock<ITeacherDocumentRepository>();
        var submissionRepo = new Mock<ITeacherRegistrationSubmissionRepository>();
        var fileStorage = new Mock<IFileStorageService>();
        var transaction = new Mock<IDbContextTransaction>();

        submissionRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        submissionRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
        submissionRepo.Setup(r => r.DeleteAllForTeacherAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        documentRepo.Setup(r => r.DeletePendingForTeacherAsync(TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        submissionRepo.Setup(r => r.AddAsync(It.IsAny<TeacherRegistrationSubmission>()))
            .ReturnsAsync((TeacherRegistrationSubmission s) => s);
        submissionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        teacherRepo.Setup(r => r.UpdateAsync(It.IsAny<Teacher>())).Returns(Task.CompletedTask);
        teacherRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var teacher = new Teacher { Id = TeacherId, Status = TeacherStatus.AwaitingDocuments };
        var majorReq = new TeacherRegistrationRequirement
        {
            Id = 2,
            Code = "major",
            IsRequired = true,
            IsActive = true,
            RequirementType = RegistrationRequirementType.Text,
        };

        var input = new TeacherRegistrationSubmissionInput
        {
            Location = TeacherLocation.InsideSaudiArabia,
            TextValuesByCode = new Dictionary<string, string?> { ["major"] = "Math" },
        };

        var service = new TeacherRegistrationSubmitService(
            teacherRepo.Object,
            documentRepo.Object,
            submissionRepo.Object,
            fileStorage.Object,
            NullLogger<TeacherRegistrationSubmitService>.Instance);

        await service.SubmitAsync(
            teacher,
            input,
            [majorReq],
            alreadySubmittedCodes: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            preserveExistingSubmissions: false,
            CancellationToken.None);

        submissionRepo.Verify(
            r => r.DeleteAllForTeacherAsync(TeacherId, It.IsAny<CancellationToken>()),
            Times.Once);
        documentRepo.Verify(
            r => r.DeletePendingForTeacherAsync(TeacherId, It.IsAny<CancellationToken>()),
            Times.Once);
        submissionRepo.Verify(
            r => r.AddAsync(It.Is<TeacherRegistrationSubmission>(s => s.TextValue == "Math")),
            Times.Once);
    }
}
