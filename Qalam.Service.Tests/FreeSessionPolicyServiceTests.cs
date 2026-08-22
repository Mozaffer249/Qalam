using Moq;
using Qalam.Data.Entity.Student;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class FreeSessionPolicyServiceTests
{
    [Fact]
    public async Task IsStudentEligibleForFreeTrialAsync_Unused_ReturnsTrue()
    {
        var studentRepo = new Mock<IStudentRepository>();
        studentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Student { Id = 1, HasUsedFreeTrialSession = false });

        var sut = new FreeSessionPolicyService(
            studentRepo.Object,
            new Mock<ITeacherRepository>().Object,
            new Mock<ITeacherLevelRepository>().Object,
            new Mock<ITeacherDomainPricingRepository>().Object);

        Assert.True(await sut.IsStudentEligibleForFreeTrialAsync(1));
    }

    [Fact]
    public async Task IsStudentEligibleForFreeTrialAsync_Used_ReturnsFalse()
    {
        var studentRepo = new Mock<IStudentRepository>();
        studentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Student { Id = 1, HasUsedFreeTrialSession = true });

        var sut = new FreeSessionPolicyService(
            studentRepo.Object,
            new Mock<ITeacherRepository>().Object,
            new Mock<ITeacherLevelRepository>().Object,
            new Mock<ITeacherDomainPricingRepository>().Object);

        Assert.False(await sut.IsStudentEligibleForFreeTrialAsync(1));
    }

    [Fact]
    public async Task TryCompleteTeacherInterviewAsync_UnlocksLowestActiveLevelForDomain()
    {
        var teacher = new Teacher { Id = 5, HasCompletedInterviewSession = false, TeacherLevelId = null };
        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(teacher);
        teacherRepo.Setup(r => r.UpdateAsync(It.IsAny<Teacher>())).Returns(Task.CompletedTask);
        teacherRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var levelRepo = new Mock<ITeacherLevelRepository>();
        levelRepo.Setup(r => r.GetStarterLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherLevel { Id = 11, Code = "starter", OrderIndex = 1, IsActive = true });

        var pricing = new TeacherDomainPricing
        {
            TeacherId = 5,
            DomainId = 3,
            HasCompletedInterviewSession = false,
            TeacherLevelId = null
        };
        var domainPricingRepo = new Mock<ITeacherDomainPricingRepository>();
        domainPricingRepo
            .Setup(r => r.GetOrCreateAsync(5, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);
        domainPricingRepo.Setup(r => r.UpdateAsync(It.IsAny<TeacherDomainPricing>())).Returns(Task.CompletedTask);

        var sut = new FreeSessionPolicyService(
            new Mock<IStudentRepository>().Object,
            teacherRepo.Object,
            levelRepo.Object,
            domainPricingRepo.Object);

        await sut.TryCompleteTeacherInterviewAsync(5, 3);

        Assert.True(pricing.HasCompletedInterviewSession);
        Assert.Equal(11, pricing.TeacherLevelId);
        Assert.True(teacher.HasCompletedInterviewSession);
        Assert.Equal(11, teacher.TeacherLevelId);
    }
}
