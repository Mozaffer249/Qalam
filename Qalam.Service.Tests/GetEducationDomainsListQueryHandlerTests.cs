using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Education.Queries.GetEducationDomainsList;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Xunit;

namespace Qalam.Service.Tests;

public class GetEducationDomainsListQueryHandlerTests
{
    [Fact]
    public async Task Handle_ForSubjectSelection_FiltersToEligibleDomainsOnly()
    {
        var domainService = new Mock<IEducationDomainService>();
        domainService
            .Setup(s => s.GetPaginatedDomainsAsync(1, 50, null))
            .ReturnsAsync(new PaginatedResult<EducationDomainDto>
            {
                PageNumber = 1,
                PageSize = 50,
                TotalCount = 2,
                Items =
                [
                    new EducationDomainDto
                    {
                        Id = 1,
                        NameAr = "مدرسة",
                        NameEn = "School",
                        Code = "school",
                        CreatedAt = DateTime.UtcNow
                    },
                    new EducationDomainDto
                    {
                        Id = 2,
                        NameAr = "قرآن",
                        NameEn = "Quran",
                        Code = "quran",
                        CreatedAt = DateTime.UtcNow
                    }
                ]
            });

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo
            .Setup(r => r.GetByUserIdAsync(99))
            .ReturnsAsync(new Data.Entity.Teacher.Teacher { Id = 7, UserId = 99 });

        var statusService = new Mock<ITeacherDomainQuestionStatusService>();
        statusService
            .Setup(s => s.EnrichDomainsForTeacherAsync(
                It.IsAny<IReadOnlyList<EducationDomainDto>>(),
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new EducationDomainTeacherDto
                {
                    Id = 1,
                    NameAr = "مدرسة",
                    NameEn = "School",
                    Code = "school",
                    CreatedAt = DateTime.UtcNow,
                    CanSelectForSubjects = true
                },
                new EducationDomainTeacherDto
                {
                    Id = 2,
                    NameAr = "قرآن",
                    NameEn = "Quran",
                    Code = "quran",
                    CreatedAt = DateTime.UtcNow,
                    CanSelectForSubjects = false
                }
            ]);

        var localizer = new Mock<IStringLocalizer<SharedResources>>();
        var handler = new GetEducationDomainsListQueryHandler(
            localizer.Object,
            domainService.Object,
            teacherRepo.Object,
            statusService.Object);

        var response = await handler.Handle(
            new GetEducationDomainsListQuery
            {
                UserId = 99,
                PageNumber = 1,
                PageSize = 50,
                ForSubjectSelection = true
            },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Single(response.Data);
        Assert.Equal("school", response.Data[0].Code);
        Assert.True(response.Data[0].CanSelectForSubjects);
    }
}
