using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

internal static class ComplaintResolutionTestHelper
{
    internal static ComplaintResolutionOrchestrator CreateOrchestrator(
        ApplicationDBContext db,
        IRefundService? refundService = null)
    {
        var audit = new SessionAuditService(new SessionAuditLogRepository(db));
        var complaintRepo = new SessionComplaintRepository(db);
        var scheduleRepo = new CourseScheduleRepository(db);
        var financeImpact = new TeacherFinanceImpactService(new TeacherFinanceImpactRepository(db));
        return new ComplaintResolutionOrchestrator(
            complaintRepo,
            scheduleRepo,
            refundService ?? new RefundService(new RefundRepository(db), financeImpact),
            audit,
            financeImpact);
    }

    internal static SessionComplaintService CreateComplaintService(
        ApplicationDBContext db,
        Mock<IRefundService>? refundMock = null)
    {
        var audit = new SessionAuditService(new SessionAuditLogRepository(db));
        var complaintRepo = new SessionComplaintRepository(db);
        var scheduleRepo = new CourseScheduleRepository(db);
        var earning = new TeacherEarningService(db, NullLogger<TeacherEarningService>.Instance);
        var refund = refundMock ?? new Mock<IRefundService>();
        var fileStorage = new Mock<IFileStorageService>();
        fileStorage
            .Setup(f => f.ValidateFileAsync(It.IsAny<IFormFile>(), It.IsAny<string[]>(), It.IsAny<long>()))
            .ReturnsAsync(true);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OssSettings:LearningPublicBaseUrl"] = "https://cdn.example.com",
            })
            .Build();
        var orchestrator = CreateOrchestrator(db, refundMock?.Object ?? new RefundService(
            new RefundRepository(db),
            new TeacherFinanceImpactService(new TeacherFinanceImpactRepository(db))));
        return new SessionComplaintService(
            complaintRepo,
            scheduleRepo,
            audit,
            earning,
            fileStorage.Object,
            config,
            orchestrator);
    }
}
