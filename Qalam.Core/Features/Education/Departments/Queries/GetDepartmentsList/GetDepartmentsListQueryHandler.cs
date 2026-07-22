using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Departments.Queries.GetDepartmentsList;

public class GetDepartmentsListQueryHandler : ResponseHandler,
    IRequestHandler<GetDepartmentsListQuery, Response<List<DepartmentDto>>>
{
    private readonly IDepartmentRepository _repo;

    public GetDepartmentsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IDepartmentRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<List<DepartmentDto>>> Handle(GetDepartmentsListQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.GetDepartmentsDtoAsync(request.CollegeId, request.ActiveOnly, cancellationToken);
        return Success(entity: list);
    }
}
