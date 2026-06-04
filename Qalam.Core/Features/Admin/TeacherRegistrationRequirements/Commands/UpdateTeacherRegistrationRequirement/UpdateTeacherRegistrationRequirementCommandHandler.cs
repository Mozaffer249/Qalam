using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.UpdateTeacherRegistrationRequirement;

public class UpdateTeacherRegistrationRequirementCommandHandler : ResponseHandler,
    IRequestHandler<UpdateTeacherRegistrationRequirementCommand, Response<TeacherRegistrationRequirementAdminDto>>
{
    private readonly ITeacherRegistrationRequirementRepository _repository;
    private readonly ITeacherRegistrationRequirementProvider _provider;

    public UpdateTeacherRegistrationRequirementCommandHandler(
        ITeacherRegistrationRequirementRepository repository,
        ITeacherRegistrationRequirementProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<TeacherRegistrationRequirementAdminDto>> Handle(
        UpdateTeacherRegistrationRequirementCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<TeacherRegistrationRequirementAdminDto>("Requirement not found");

        var dto = request.Data;
        if (dto.MaxCount < dto.MinCount)
            return BadRequest<TeacherRegistrationRequirementAdminDto>("MaxCount must be >= MinCount.");

        entity.NameAr = dto.NameAr.Trim();
        entity.NameEn = dto.NameEn.Trim();
        entity.DescriptionAr = dto.DescriptionAr?.Trim();
        entity.DescriptionEn = dto.DescriptionEn?.Trim();
        entity.IsActive = dto.IsActive;
        entity.IsRequired = dto.IsRequired;
        entity.SortOrder = dto.SortOrder;
        entity.MinCount = dto.MinCount;
        entity.MaxCount = dto.MaxCount;
        entity.MaxFileSizeBytes = dto.MaxFileSizeBytes;
        entity.MaxLength = dto.MaxLength;

        if (dto.AllowedExtensions != null && entity.RequirementType == RegistrationRequirementType.File)
            entity.AllowedExtensionsJson = RegistrationRequirementExtensionsHelper.ToJson(dto.AllowedExtensions);

        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        return Success("Requirement updated", entity: _provider.ToAdminDto(entity));
    }
}
