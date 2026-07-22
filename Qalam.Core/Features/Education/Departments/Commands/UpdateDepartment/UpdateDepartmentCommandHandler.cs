using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Departments.Commands.UpdateDepartment;

public class UpdateDepartmentCommandHandler : ResponseHandler,
    IRequestHandler<UpdateDepartmentCommand, Response<DepartmentDto>>
{
    private readonly IDepartmentRepository _repo;
    private readonly ICollegeRepository _colleges;

    public UpdateDepartmentCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IDepartmentRepository repo,
        ICollegeRepository colleges) : base(localizer)
    {
        _repo = repo;
        _colleges = colleges;
    }

    public async Task<Response<DepartmentDto>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return NotFound<DepartmentDto>("Department not found");

        if (!await _colleges.ExistsAsync(request.CollegeId, cancellationToken))
            return BadRequest<DepartmentDto>("College not found");

        existing.CollegeId = request.CollegeId;
        existing.NameAr = request.NameAr;
        existing.NameEn = request.NameEn;
        existing.Code = request.Code;
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(existing);

        var dto = await _repo.GetDepartmentDtoByIdAsync(existing.Id, cancellationToken);
        return Success(entity: dto!);
    }
}
