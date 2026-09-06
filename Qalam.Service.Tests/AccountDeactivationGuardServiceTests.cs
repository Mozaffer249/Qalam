using Moq;
using Qalam.Data.DTOs.Account;
using Qalam.Data.Entity.Student;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class AccountDeactivationGuardServiceTests
{
    private static AccountDeactivationGuardService Create(
        Mock<IEnrollmentRepository>? enrollments = null,
        Mock<IOpenSessionRequestRepository>? osr = null,
        Mock<ICourseEnrollmentRequestRepository>? requests = null)
    {
        var students = new Mock<IStudentRepository>();
        students.Setup(s => s.GetByUserIdAsync(5))
            .ReturnsAsync(new Student { Id = 10, UserId = 5, GuardianId = null });
        students.Setup(s => s.GetChildrenByGuardianIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Student>());

        var guardians = new Mock<IGuardianRepository>();
        guardians.Setup(g => g.GetByUserIdAsync(5)).ReturnsAsync((Guardian?)null);

        if (enrollments == null)
        {
            enrollments = new Mock<IEnrollmentRepository>();
            enrollments.Setup(e => e.AnyActiveOrPendingPaymentAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        if (osr == null)
        {
            osr = new Mock<IOpenSessionRequestRepository>();
            osr.Setup(o => o.AnyBlockingForUserAsync(5, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            osr.Setup(o => o.AnyBlockingInvitationsForUserAsync(5, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        if (requests == null)
        {
            requests = new Mock<ICourseEnrollmentRequestRepository>();
            requests.Setup(r => r.AnyBlockingInvitationsForUserAsync(5, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        return new AccountDeactivationGuardService(
            students.Object,
            guardians.Object,
            enrollments.Object,
            osr.Object,
            requests.Object);
    }

    [Fact]
    public async Task GetBlockingReasons_WhenClear_ReturnsEmpty()
    {
        var service = Create();
        var reasons = await service.GetBlockingReasonsAsync(5);
        Assert.Empty(reasons);
    }

    [Fact]
    public async Task GetBlockingReasons_WhenActiveEnrollment_ReturnsActiveEnrollment()
    {
        var enrollments = new Mock<IEnrollmentRepository>();
        enrollments.Setup(e => e.AnyActiveOrPendingPaymentAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = Create(enrollments: enrollments);
        var reasons = await service.GetBlockingReasonsAsync(5);

        Assert.Contains(AccountDeactivationBlockingReasons.ActiveEnrollment, reasons);
    }

    [Fact]
    public async Task GetBlockingReasons_WhenOpenOsr_ReturnsOpenSessionRequest()
    {
        var osr = new Mock<IOpenSessionRequestRepository>();
        osr.Setup(o => o.AnyBlockingForUserAsync(5, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        osr.Setup(o => o.AnyBlockingInvitationsForUserAsync(5, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = Create(osr: osr);
        var reasons = await service.GetBlockingReasonsAsync(5);

        Assert.Contains(AccountDeactivationBlockingReasons.OpenSessionRequest, reasons);
    }

    [Fact]
    public async Task GetBlockingReasons_WhenPendingS1Invite_ReturnsPendingInvitation()
    {
        var requests = new Mock<ICourseEnrollmentRequestRepository>();
        requests.Setup(r => r.AnyBlockingInvitationsForUserAsync(5, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = Create(requests: requests);
        var reasons = await service.GetBlockingReasonsAsync(5);

        Assert.Contains(AccountDeactivationBlockingReasons.PendingInvitation, reasons);
    }

    [Fact]
    public async Task GetBlockingReasons_WhenPendingS2Invite_ReturnsPendingInvitation()
    {
        var osr = new Mock<IOpenSessionRequestRepository>();
        osr.Setup(o => o.AnyBlockingForUserAsync(5, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        osr.Setup(o => o.AnyBlockingInvitationsForUserAsync(5, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = Create(osr: osr);
        var reasons = await service.GetBlockingReasonsAsync(5);

        Assert.Contains(AccountDeactivationBlockingReasons.PendingInvitation, reasons);
    }
}
