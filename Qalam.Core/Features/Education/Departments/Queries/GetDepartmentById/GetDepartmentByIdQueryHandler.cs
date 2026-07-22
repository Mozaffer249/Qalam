using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Departments.Queries.GetDepartmentById;

public class GetDepartmentByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetDepartmentByIdQuery, Response<DepartmentDto>>
{
    private readonly IDepartmentRepository _repo;

    public GetDepartmentByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IDepartmentRepository repo) : base(localizer)
    {
        _repo = repo;
    }

    public async Task<Response<DepartmentDto>> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repo.GetDepartmentDtoByIdAsync(request.Id, cancellationToken);
        return item is null ? NotFound<DepartmentDto>("Department not found") : Success(entity: item);
    }
}
