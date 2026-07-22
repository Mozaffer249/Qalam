using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.AcademicPrograms.Commands.CreateAcademicProgram;

public class CreateAcademicProgramCommandHandler : ResponseHandler,
    IRequestHandler<CreateAcademicProgramCommand, Response<AcademicProgramDto>>
{
    private readonly IAcademicProgramRepository _repo;
    private readonly IDepartmentRepository _departments;

    public CreateAcademicProgramCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IAcademicProgramRepository repo,
        IDepartmentRepository departments) : base(localizer)
    {
        _repo = repo;
        _departments = departments;
    }

    public async Task<Response<AcademicProgramDto>> Handle(CreateAcademicProgramCommand request, CancellationToken cancellationToken)
    {
        if (!await _departments.ExistsAsync(request.DepartmentId, cancellationToken))
            return BadRequest<AcademicProgramDto>("Department not found");

        var entity = new AcademicProgram
        {
            DepartmentId = request.DepartmentId,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Code = request.Code,
            DegreeType = request.DegreeType,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
        };
        await _repo.AddAsync(entity);
        var dto = await _repo.GetProgramDtoByIdAsync(entity.Id, cancellationToken);
        return Created(entity: dto!);
    }
}
