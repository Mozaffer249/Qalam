using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.AcademicPrograms.Commands.UpdateAcademicProgram;

public class UpdateAcademicProgramCommandHandler : ResponseHandler,
    IRequestHandler<UpdateAcademicProgramCommand, Response<AcademicProgramDto>>
{
    private readonly IAcademicProgramRepository _repo;
    private readonly IDepartmentRepository _departments;

    public UpdateAcademicProgramCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IAcademicProgramRepository repo,
        IDepartmentRepository departments) : base(localizer)
    {
        _repo = repo;
        _departments = departments;
    }

    public async Task<Response<AcademicProgramDto>> Handle(UpdateAcademicProgramCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return NotFound<AcademicProgramDto>("Academic program not found");

        if (!await _departments.ExistsAsync(request.DepartmentId, cancellationToken))
            return BadRequest<AcademicProgramDto>("Department not found");

        existing.DepartmentId = request.DepartmentId;
        existing.NameAr = request.NameAr;
        existing.NameEn = request.NameEn;
        existing.Code = request.Code;
        existing.DegreeType = request.DegreeType;
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(existing);

        var dto = await _repo.GetProgramDtoByIdAsync(existing.Id, cancellationToken);
        return Success(entity: dto!);
    }
}
