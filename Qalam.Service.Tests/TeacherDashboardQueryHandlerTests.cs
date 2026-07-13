using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Authentication.Queries.GetProfile;
using Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceSummary;
using Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceTransactions;
using Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestsSummary;
using Qalam.Core.Features.Teacher.Profile.Queries.GetMyTeacherProfile;
using Qalam.Core.Features.Teacher.Sessions.Queries.GetMySessions;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using System.Security.Claims;
using Xunit;
using TeacherEntity = Qalam.Data.Entity.Teacher.Teacher;

namespace Qalam.Service.Tests;

public class TeacherDashboardQueryHandlerTests
{
    private static Mock<IStringLocalizer<SharedResources>> CreateSharedLocalizer()
    {
        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        return localizer;
    }

    [Fact]
    public async Task GetMyTeacherProfile_ReturnsNotFound_WhenTeacherMissing()
    {
        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync((TeacherEntity?)null);

        var userManager = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        var handler = new GetMyTeacherProfileQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            userManager.Object);

        var response = await handler.Handle(
            new GetMyTeacherProfileQuery { UserId = 42 },
            CancellationToken.None);

        Assert.False(response.Succeeded);
    }

    [Fact]
    public async Task GetMyTeacherProfile_ReturnsProfile_WhenTeacherFound()
    {
        var teacher = new TeacherEntity
        {
            Id = 7,
            UserId = 42,
            Bio = "Math teacher",
            Location = TeacherLocation.InsideSaudiArabia,
            Status = TeacherStatus.Active,
            RatingAverage = 4.5m,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var user = new User
        {
            Id = 42,
            FirstName = "Sara",
            LastName = "Ali",
            Email = "sara@example.com",
            PhoneNumber = "+966500000000",
        };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(teacher);

        var userManager = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(m => m.FindByIdAsync("42")).ReturnsAsync(user);

        var handler = new GetMyTeacherProfileQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            userManager.Object);

        var response = await handler.Handle(
            new GetMyTeacherProfileQuery { UserId = 42 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(7, response.Data!.TeacherId);
        Assert.Equal("Sara Ali", response.Data.FullName);
        Assert.Equal("Math teacher", response.Data.Bio);
    }

    [Fact]
    public async Task GetAvailableRequestsSummary_ReturnsCounts_ForActiveTeacher()
    {
        var teacher = new TeacherEntity { Id = 3, UserId = 10, Status = TeacherStatus.Active };
        var counts = new TeacherInboxCountsDto
        {
            All = 5,
            Notified = 2,
            Viewed = 1,
            OfferSubmitted = 1,
            Skipped = 1,
        };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(teacher);

        var targetRepo = new Mock<IOpenSessionRequestTargetRepository>();
        targetRepo
            .Setup(r => r.GetTeacherInboxCountsAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);

        var handler = new GetAvailableRequestsSummaryQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            targetRepo.Object);

        var response = await handler.Handle(
            new GetAvailableRequestsSummaryQuery { UserId = 10 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(2, response.Data!.Counts.Notified);
        Assert.Equal(5, response.Data.Counts.All);
    }

    [Fact]
    public async Task GetMySessions_PassesUpcomingFilterToRepository()
    {
        var teacher = new TeacherEntity { Id = 8, UserId = 15 };
        var sessions = new List<TeacherMySessionListItemDto>
        {
            new()
            {
                Id = 101,
                CourseTitle = "Algebra",
                SessionTitle = "Session 1",
                StartsAt = DateTime.UtcNow.AddDays(1),
                Status = "Scheduled",
            },
        };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(15)).ReturnsAsync(teacher);

        var dashboardRepo = new Mock<ITeacherDashboardReadRepository>();
        dashboardRepo
            .Setup(r => r.GetMySessionsAsync(8, "upcoming", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var handler = new GetMySessionsQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            dashboardRepo.Object);

        var response = await handler.Handle(
            new GetMySessionsQuery { UserId = 15, Filter = "upcoming", PageSize = 10 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Single(response.Data!);
        Assert.Equal(101, response.Data[0].Id);
        dashboardRepo.Verify(r => r.GetMySessionsAsync(8, "upcoming", 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFinanceSummary_ReturnsZeroDefaults_WhenNoPayments()
    {
        var teacher = new TeacherEntity { Id = 4, UserId = 20 };
        var summary = new TeacherFinanceSummaryDto();

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(20)).ReturnsAsync(teacher);

        var dashboardRepo = new Mock<ITeacherDashboardReadRepository>();
        dashboardRepo
            .Setup(r => r.GetFinanceSummaryAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var handler = new GetFinanceSummaryQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            dashboardRepo.Object);

        var response = await handler.Handle(
            new GetFinanceSummaryQuery { UserId = 20 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(0, response.Data!.EarningsThisMonth);
        Assert.Equal(0, response.Data.TotalEarningsAllTime);
        Assert.Equal(0, response.Data.TransactionsCount);
    }

    [Fact]
    public async Task GetFinanceTransactions_ReturnsEmptyPage_WhenNoPayments()
    {
        var teacher = new TeacherEntity { Id = 4, UserId = 20 };
        var emptyPage = new PaginatedResult<TeacherFinanceTransactionDto>
        {
            Items = [],
            PageNumber = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0,
        };

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(20)).ReturnsAsync(teacher);

        var dashboardRepo = new Mock<ITeacherDashboardReadRepository>();
        dashboardRepo
            .Setup(r => r.GetFinanceTransactionsAsync(4, "all", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPage);

        var handler = new GetFinanceTransactionsQueryHandler(
            CreateSharedLocalizer().Object,
            teacherRepo.Object,
            dashboardRepo.Object);

        var response = await handler.Handle(
            new GetFinanceTransactionsQuery { UserId = 20, Filter = "all", PageNumber = 1, PageSize = 20 },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Empty(response.Data!.Items);
        Assert.Equal(0, response.Data.TotalCount);
    }

    [Fact]
    public async Task GetProfile_ReturnsCurrentUser_WhenAuthenticated()
    {
        var user = new User
        {
            Id = 99,
            UserName = "teacher1",
            Email = "teacher1@example.com",
            FirstName = "Ahmed",
            LastName = "Hassan",
            PhoneNumber = "+966511111111",
        };

        var userManager = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync(user);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "99"),
        ], authenticationType: "Test"));

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var authLocalizer = new Mock<IStringLocalizer<Qalam.Core.Resources.Authentication.AuthenticationResources>>();
        authLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var handler = new GetProfileQueryHandler(
            userManager.Object,
            httpContextAccessor.Object,
            authLocalizer.Object);

        var response = await handler.Handle(new GetProfileQuery(), CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal("Ahmed", response.Data!.FirstName);
        Assert.Equal("teacher1@example.com", response.Data.Email);
    }
}
