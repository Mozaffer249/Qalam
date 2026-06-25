using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.CreateTeacherRegistrationRequirement;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherDomainQuestions.Commands.CreateTeacherDomainQuestion;

public class CreateTeacherDomainQuestionCommandHandler : ResponseHandler,
    IRequestHandler<CreateTeacherDomainQuestionCommand, Response<TeacherDomainQuestionAdminDto>>
{
    private readonly ITeacherDomainQuestionRepository _repository;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ITeacherDomainQuestionProvider _provider;

    public CreateTeacherDomainQuestionCommandHandler(
        ITeacherDomainQuestionRepository repository,
        IEducationDomainRepository domainRepository,
        ITeacherDomainQuestionProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _domainRepository = domainRepository;
        _provider = provider;
    }

    public async Task<Response<TeacherDomainQuestionAdminDto>> Handle(
        CreateTeacherDomainQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Data;
        var code = dto.Code.Trim().ToLowerInvariant();

        var domain = await _domainRepository.GetByIdAsync(dto.DomainId);
        if (domain == null)
            return BadRequest<TeacherDomainQuestionAdminDto>("Education domain not found.");

        if (await _repository.CodeExistsInDomainAsync(dto.DomainId, code, cancellationToken: cancellationToken))
            return BadRequest<TeacherDomainQuestionAdminDto>("Code already exists in this domain.");

        var extensions = dto.AllowedExtensions?.Count > 0
            ? RegistrationRequirementExtensionsHelper.ToJson(dto.AllowedExtensions)
            : RegistrationRequirementExtensionsHelper.ToJson(new[] { ".pdf", ".jpg", ".jpeg", ".png" });

        string? optionsJson = null;
        if (dto.RequirementType == RegistrationRequirementType.Selection)
        {
            var optionError = CreateTeacherRegistrationRequirementCommandHandler
                .ValidateSelectionOptions(dto.Options, dto.MinCount, dto.MaxCount);
            if (optionError != null)
                return BadRequest<TeacherDomainQuestionAdminDto>(optionError);

            optionsJson = RegistrationRequirementOptionsHelper.Serialize(
                dto.Options!.Select(o => new RequirementOption(o.Value.Trim(), o.LabelAr.Trim(), o.LabelEn.Trim())));
        }

        var entity = new TeacherDomainQuestion
        {
            DomainId = dto.DomainId,
            Code = code,
            NameAr = dto.NameAr.Trim(),
            NameEn = dto.NameEn.Trim(),
            DescriptionAr = dto.DescriptionAr?.Trim(),
            DescriptionEn = dto.DescriptionEn?.Trim(),
            RequirementType = dto.RequirementType,
            IsActive = dto.IsActive,
            IsRequired = dto.IsRequired,
            RequiresAdminReview = dto.RequiresAdminReview,
            SortOrder = dto.SortOrder,
            MinCount = dto.MinCount,
            MaxCount = dto.MaxCount,
            MaxFileSizeBytes = dto.MaxFileSizeBytes,
            AllowedExtensionsJson = extensions,
            MaxLength = dto.MaxLength,
            OptionsJson = optionsJson,
            MapsToDocumentType = dto.MapsToDocumentType,
            IsSystem = false,
            Domain = domain
        };

        if (entity.RequirementType != RegistrationRequirementType.File)
        {
            entity.MapsToDocumentType = null;
            entity.AllowedExtensionsJson = "[]";
        }

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return Success("Domain question created", entity: _provider.ToAdminDto(entity));
    }
}
