using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.MiddleWare;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Xunit;

namespace Qalam.Service.Tests;

public class BlockedTeacherMiddlewareTests
{
    private const int UserId = 42;
    private const string BlockedMessage = "Your account has been blocked. Please contact support.";

    [Fact]
    public async Task InvokeAsync_Returns403_WhenTeacherIsBlocked()
    {
        var nextCalled = false;
        var teacherRepo = CreateTeacherRepo(new Teacher { Id = 1, UserId = UserId, Status = TeacherStatus.Blocked });
        var middleware = CreateMiddleware(_ => nextCalled = true, teacherRepo);

        var context = CreateAuthenticatedContext(UserId);
        await middleware.InvokeAsync(context, teacherRepo.Object, CreateLocalizer());

        Assert.False(nextCalled);
        Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.False(doc.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Equal(BlockedMessage, doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvokeAsync_Continues_WhenTeacherIsActive()
    {
        var nextCalled = false;
        var teacherRepo = CreateTeacherRepo(new Teacher { Id = 1, UserId = UserId, Status = TeacherStatus.Active });
        var middleware = CreateMiddleware(_ => nextCalled = true, teacherRepo);

        await middleware.InvokeAsync(
            CreateAuthenticatedContext(UserId),
            teacherRepo.Object,
            CreateLocalizer());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Continues_WhenNoTeacherProfile()
    {
        var nextCalled = false;
        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync((Teacher?)null);
        var middleware = CreateMiddleware(_ => nextCalled = true, teacherRepo);

        await middleware.InvokeAsync(
            CreateAuthenticatedContext(UserId),
            teacherRepo.Object,
            CreateLocalizer());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Continues_WhenUnauthenticated()
    {
        var nextCalled = false;
        var teacherRepo = new Mock<ITeacherRepository>();
        var middleware = CreateMiddleware(_ => nextCalled = true, teacherRepo);

        await middleware.InvokeAsync(
            new DefaultHttpContext(),
            teacherRepo.Object,
            CreateLocalizer());

        Assert.True(nextCalled);
        teacherRepo.Verify(r => r.GetByUserIdAsync(It.IsAny<int>()), Times.Never);
    }

    private static BlockedTeacherMiddleware CreateMiddleware(
        Action<HttpContext> onNext,
        Mock<ITeacherRepository> teacherRepo)
    {
        return new BlockedTeacherMiddleware(ctx =>
        {
            onNext(ctx);
            return Task.CompletedTask;
        });
    }

    private static Mock<ITeacherRepository> CreateTeacherRepo(Teacher teacher)
    {
        var repo = new Mock<ITeacherRepository>();
        repo.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(teacher);
        return repo;
    }

    private static DefaultHttpContext CreateAuthenticatedContext(int userId)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], authenticationType: "Test"));
        return context;
    }

    private static IStringLocalizer<AuthenticationResources> CreateLocalizer()
    {
        var localizer = new Mock<IStringLocalizer<AuthenticationResources>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, BlockedMessage));
        return localizer.Object;
    }
}
