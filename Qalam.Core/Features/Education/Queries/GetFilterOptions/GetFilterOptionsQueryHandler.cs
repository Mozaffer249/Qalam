using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetFilterOptions;

public class GetFilterOptionsQueryHandler : ResponseHandler,
    IRequestHandler<GetFilterOptionsQuery, Response<FilterOptionsResponseDto>>
{
    private readonly IEducationFilterService _filterService;

    public GetFilterOptionsQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationFilterService filterService) : base(localizer)
    {
        _filterService = filterService;
    }

    public async Task<Response<FilterOptionsResponseDto>> Handle(
        GetFilterOptionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var state = new FilterStateDto
            {
                DomainId = request.DomainId,
                CurriculumId = request.CurriculumId,
                LevelId = request.LevelId,
                GradeId = request.GradeId,
                TermIds = request.TermIds,  
                SubjectId = request.SubjectId,
                QuranContentTypeId = request.QuranContentTypeId,
                QuranLevelId = request.QuranLevelId,
                UnitTypeCode = request.UnitTypeCode
            };

            var result = await _filterService.GetFilterOptionsAsync(state, request.PageNumber, request.PageSize);
            return Success(entity: result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest<FilterOptionsResponseDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound<FilterOptionsResponseDto>(ex.Message);
        }
    }
}
