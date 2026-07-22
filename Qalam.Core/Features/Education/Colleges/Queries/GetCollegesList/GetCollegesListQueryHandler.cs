using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Colleges.Queries.GetCollegesList;

public class GetCollegesListQueryHandler : ResponseHandler,
    IRequestHandler<GetCollegesListQuery, Response<List<CollegeDto>>>
{
    private readonly ICollegeRepository _repo;

    public GetCollegesListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ICollegeRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<List<CollegeDto>>> Handle(GetCollegesListQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.GetCollegesDtoAsync(request.UniversityId, request.ActiveOnly, cancellationToken);
        return Success(entity: list);
    }
}
