using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.AcademicPrograms.Queries.GetAcademicProgramById;

public class GetAcademicProgramByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetAcademicProgramByIdQuery, Response<AcademicProgramDto>>
{
    private readonly IAcademicProgramRepository _repo;

    public GetAcademicProgramByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IAcademicProgramRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<AcademicProgramDto>> Handle(GetAcademicProgramByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repo.GetProgramDtoByIdAsync(request.Id, cancellationToken);
        return item is null ? NotFound<AcademicProgramDto>("Academic program not found") : Success(entity: item);
    }
}
