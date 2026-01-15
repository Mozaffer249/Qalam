using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Curriculum.Queries.GetCurriculumById;

public class GetCurriculumByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetCurriculumByIdQuery, Response<CurriculumDto>>
{
    private readonly ICurriculumService _curriculumService;

    public GetCurriculumByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ICurriculumService curriculumService) : base(localizer)
    {
        _curriculumService = curriculumService;
    }

    public async Task<Response<CurriculumDto>> Handle(
        GetCurriculumByIdQuery request,
        CancellationToken cancellationToken)
    {
        var curriculum = await _curriculumService.GetCurriculumDtoByIdAsync(request.Id);

        if (curriculum == null)
            return NotFound<CurriculumDto>("Curriculum not found");

        return Success(entity: curriculum);
    }
}
