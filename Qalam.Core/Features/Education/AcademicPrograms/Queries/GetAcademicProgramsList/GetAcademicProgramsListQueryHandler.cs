using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.AcademicPrograms.Queries.GetAcademicProgramsList;

public class GetAcademicProgramsListQueryHandler : ResponseHandler,
    IRequestHandler<GetAcademicProgramsListQuery, Response<List<AcademicProgramDto>>>
{
    private readonly IAcademicProgramRepository _repo;

    public GetAcademicProgramsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IAcademicProgramRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<List<AcademicProgramDto>>> Handle(GetAcademicProgramsListQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.GetProgramsDtoAsync(request.DepartmentId, request.ActiveOnly, cancellationToken);
        return Success(entity: list);
    }
}
