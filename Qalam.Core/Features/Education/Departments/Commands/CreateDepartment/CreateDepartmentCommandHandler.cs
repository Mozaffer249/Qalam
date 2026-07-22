using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Departments.Commands.CreateDepartment;

public class CreateDepartmentCommandHandler : ResponseHandler,
    IRequestHandler<CreateDepartmentCommand, Response<DepartmentDto>>
{
    private readonly IDepartmentRepository _repo;
    private readonly ICollegeRepository _colleges;

    public CreateDepartmentCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IDepartmentRepository repo,
        ICollegeRepository colleges) : base(localizer)
    {
        _repo = repo;
        _colleges = colleges;
    }

    public async Task<Response<DepartmentDto>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (!await _colleges.ExistsAsync(request.CollegeId, cancellationToken))
            return BadRequest<DepartmentDto>("College not found");

        var entity = new Department
        {
            CollegeId = request.CollegeId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Code = request.Code,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
        };
        await _repo.AddAsync(entity);
        var dto = await _repo.GetDepartmentDtoByIdAsync(entity.Id, cancellationToken);
        return Created(entity: dto!);
    }
}
