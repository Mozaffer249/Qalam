using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.CreateTeacherRegistrationRequirement;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.UpdateTeacherDomainQuestion;

public class UpdateTeacherDomainQuestionCommandHandler : ResponseHandler,
    IRequestHandler<UpdateTeacherDomainQuestionCommand, Response<TeacherDomainQuestionAdminDto>>
{
    private readonly ITeacherDomainQuestionRepository _repository;
    private readonly ITeacherDomainQuestionProvider _provider;

    public UpdateTeacherDomainQuestionCommandHandler(
        ITeacherDomainQuestionRepository repository,
        ITeacherDomainQuestionProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<TeacherDomainQuestionAdminDto>> Handle(
        UpdateTeacherDomainQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdWithDomainAsync(request.Id, cancellationToken);
        if (entity == null)
            return NotFound<TeacherDomainQuestionAdminDto>("Domain question not found");

        var dto = request.Data;
        if (dto.MaxCount < dto.MinCount)
            return BadRequest<TeacherDomainQuestionAdminDto>("MaxCount must be >= MinCount.");

        entity.NameAr = dto.NameAr.Trim();
        entity.NameEn = dto.NameEn.Trim();
        entity.DescriptionAr = dto.DescriptionAr?.Trim();
        entity.DescriptionEn = dto.DescriptionEn?.Trim();
        entity.IsActive = dto.IsActive;
        entity.IsRequired = dto.IsRequired;
        entity.RequiresAdminReview = dto.RequiresAdminReview;
        entity.SortOrder = dto.SortOrder;
        entity.MinCount = dto.MinCount;
        entity.MaxCount = dto.MaxCount;
        entity.MaxFileSizeBytes = dto.MaxFileSizeBytes;
        entity.MaxLength = dto.MaxLength;

        if (dto.AllowedExtensions != null && entity.RequirementType == RegistrationRequirementType.File)
            entity.AllowedExtensionsJson = RegistrationRequirementExtensionsHelper.ToJson(dto.AllowedExtensions);

        if (entity.RequirementType == RegistrationRequirementType.Selection && dto.Options != null)
        {
            var optionError = CreateTeacherRegistrationRequirementCommandHandler
                .ValidateSelectionOptions(dto.Options, dto.MinCount, dto.MaxCount);
            if (optionError != null)
                return BadRequest<TeacherDomainQuestionAdminDto>(optionError);

            entity.OptionsJson = RegistrationRequirementOptionsHelper.Serialize(
                dto.Options.Select(o => new RequirementOption(o.Value.Trim(), o.LabelAr.Trim(), o.LabelEn.Trim())));
        }

        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        return Success("Domain question updated", entity: _provider.ToAdminDto(entity));
    }
}
