using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PaymentConfirmationConcurrencyTests
{
    [Fact]
    public async Task ConfirmAsync_ConcurrentConfirmAlreadyActivated_DoesNotCreateSchedulesAgain()
    {
        // This request loaded the enrollment before a concurrent confirm (webhook / client) committed:
        // tracked graph says PendingPayment with no schedules, the database says Active with 5.
        var payment = new Payment
        {
            Id = 10,
            Status = PaymentStatus.Succeeded,
            TotalAmount = 300m,
            Currency = "SAR",
            PaymentItems = [new PaymentItem { ItemType = PaymentItemType.CourseEnrollment, ReferenceId = 20 }],
        };
        var enrollment = new Enrollment
        {
            Id = 20,
            EnrollmentStatus = EnrollmentStatus.PendingPayment,
            Participants = [new EnrollmentParticipant { Id = 1, PaymentStatus = PaymentStatus.Pending }],
        };

        var paymentRepo = new Mock<IPaymentRepository>();
        paymentRepo.Setup(r => r.GetByIdWithItemsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var enrollmentRepo = new Mock<IEnrollmentRepository>();
        enrollmentRepo.Setup(r => r.GetByIdForPaymentAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        enrollmentRepo
            .Setup(r => r.GetCommittedActivationStateAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentStatus.Active, 5));

        var enrollmentPaymentRepo = new Mock<IEnrollmentPaymentRepository>();
        var scheduleGenerator = new Mock<IScheduleGenerationService>(MockBehavior.Strict);

        var sut = new PaymentConfirmationService(
            paymentRepo.Object,
            enrollmentRepo.Object,
            enrollmentPaymentRepo.Object,
            Mock.Of<ITeacherAvailabilityRepository>(),
            Mock.Of<ICourseScheduleRepository>(),
            scheduleGenerator.Object,
            Mock.Of<IOpenSessionRequestReleaseService>(),
            Mock.Of<IRefundService>(),
            Mock.Of<IPaymentGatewayResolver>(),
            Mock.Of<IPaymentTransactionEventService>(),
            NullLogger<PaymentConfirmationService>.Instance);

        var outcome = await sut.ConfirmAsync(10);

        Assert.True(outcome.Succeeded);
        Assert.Equal(5, outcome.Result!.SchedulesCreated);
        Assert.Empty(enrollment.CourseSchedules);
        enrollmentRepo.Verify(r => r.AcquirePaymentConfirmationLockAsync(20, It.IsAny<CancellationToken>()), Times.Once);
        enrollmentPaymentRepo.Verify(r => r.AddAsync(It.IsAny<EnrollmentPayment>()), Times.Never);
        paymentRepo.Verify(r => r.CommitAsync(), Times.Once);
    }
}
