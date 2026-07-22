using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Colleges.Queries.GetCollegeById;

public class GetCollegeByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetCollegeByIdQuery, Response<CollegeDto>>
{
    private readonly ICollegeRepository _repo;

    public GetCollegeByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        ICollegeRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<CollegeDto>> Handle(GetCollegeByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repo.GetCollegeDtoByIdAsync(request.Id, cancellationToken);
        return item is null ? NotFound<CollegeDto>("College not found") : Success(entity: item);
    }
}
