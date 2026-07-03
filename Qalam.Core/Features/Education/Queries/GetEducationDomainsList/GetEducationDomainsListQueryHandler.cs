using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainsList;

public class GetEducationDomainsListQueryHandler : ResponseHandler,
    IRequestHandler<GetEducationDomainsListQuery, Response<List<EducationDomainTeacherDto>>>
{
    private readonly IEducationDomainService _domainService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDomainQuestionStatusService _domainQuestionStatusService;

    public GetEducationDomainsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService,
        ITeacherRepository teacherRepository,
        ITeacherDomainQuestionStatusService domainQuestionStatusService) : base(localizer)
    {
        _domainService = domainService;
        _teacherRepository = teacherRepository;
        _domainQuestionStatusService = domainQuestionStatusService;
    }

    public async Task<Response<List<EducationDomainTeacherDto>>> Handle(
        GetEducationDomainsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _domainService.GetPaginatedDomainsAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        List<EducationDomainTeacherDto> items;

        if (request.UserId > 0)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
            if (teacher != null)
            {
                items = await _domainQuestionStatusService.EnrichDomainsForTeacherAsync(
                    result.Items,
                    teacher.Id,
                    cancellationToken);
            }
            else
            {
                items = result.Items.Select(MapWithoutQuestions).ToList();
            }
        }
        else
        {
            items = result.Items.Select(MapWithoutQuestions).ToList();
        }

        if (request.ForSubjectSelection && request.UserId > 0)
        {
            items = items.Where(d => d.CanSelectForSubjects).ToList();
        }

        return Success(
            entity: items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }

    private static EducationDomainTeacherDto MapWithoutQuestions(Data.DTOs.EducationDomainDto domain) =>
        new()
        {
            Id = domain.Id,
            NameAr = domain.NameAr,
            NameEn = domain.NameEn,
            Code = domain.Code,
            DescriptionAr = domain.DescriptionAr,
            DescriptionEn = domain.DescriptionEn,
            CreatedAt = domain.CreatedAt,
            RequiresAnswer = false,
            CanSelectForSubjects = false,
            Questions = new List<TeacherDomainQuestionPublicDto>()
        };
}
