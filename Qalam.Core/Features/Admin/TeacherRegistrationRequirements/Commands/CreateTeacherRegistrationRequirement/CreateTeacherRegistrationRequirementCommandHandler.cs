using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.CreateTeacherRegistrationRequirement;

public class CreateTeacherRegistrationRequirementCommandHandler : ResponseHandler,
    IRequestHandler<CreateTeacherRegistrationRequirementCommand, Response<TeacherRegistrationRequirementAdminDto>>
{
    private readonly ITeacherRegistrationRequirementRepository _repository;
    private readonly ITeacherRegistrationRequirementProvider _provider;

    public CreateTeacherRegistrationRequirementCommandHandler(
        ITeacherRegistrationRequirementRepository repository,
        ITeacherRegistrationRequirementProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<TeacherRegistrationRequirementAdminDto>> Handle(
        CreateTeacherRegistrationRequirementCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Data;
        var code = dto.Code.Trim().ToLowerInvariant();

        if (await _repository.CodeExistsAsync(code, cancellationToken: cancellationToken))
            return BadRequest<TeacherRegistrationRequirementAdminDto>("Code already exists.");

        var extensions = dto.AllowedExtensions?.Count > 0
            ? RegistrationRequirementExtensionsHelper.ToJson(dto.AllowedExtensions)
            : RegistrationRequirementExtensionsHelper.ToJson(new[] { ".pdf", ".jpg", ".jpeg", ".png" });

        string? optionsJson = null;
        if (dto.RequirementType == RegistrationRequirementType.Selection)
        {
            var optionError = ValidateSelectionOptions(dto.Options, dto.MinCount, dto.MaxCount);
            if (optionError != null)
                return BadRequest<TeacherRegistrationRequirementAdminDto>(optionError);

            optionsJson = RegistrationRequirementOptionsHelper.Serialize(
                dto.Options!.Select(o => new RequirementOption(o.Value.Trim(), o.LabelAr.Trim(), o.LabelEn.Trim())));
        }

        var entity = new TeacherRegistrationRequirement
        {
            Code = code,
            NameAr = dto.NameAr.Trim(),
            NameEn = dto.NameEn.Trim(),
            DescriptionAr = dto.DescriptionAr?.Trim(),
            DescriptionEn = dto.DescriptionEn?.Trim(),
            RequirementType = dto.RequirementType,
            IsActive = dto.IsActive,
            IsRequired = dto.IsRequired,
            SortOrder = dto.SortOrder,
            MinCount = dto.MinCount,
            MaxCount = dto.MaxCount,
            MaxFileSizeBytes = dto.MaxFileSizeBytes,
            AllowedExtensionsJson = extensions,
            MaxLength = dto.MaxLength,
            OptionsJson = optionsJson,
            MapsToDocumentType = dto.MapsToDocumentType,
            IsSystem = false
        };

        if (entity.RequirementType != RegistrationRequirementType.File)
        {
            entity.MapsToDocumentType = null;
            entity.AllowedExtensionsJson = "[]";
        }

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        return Success("Requirement created", entity: _provider.ToAdminDto(entity));
    }

    /// <summary>
    /// Shared by Create + Update handlers. Returns null when valid, otherwise the user-facing error.
    /// </summary>
    internal static string? ValidateSelectionOptions(List<RequirementOptionDto>? options, int minCount, int maxCount)
    {
        if (options == null || options.Count == 0)
            return "Selection requirement must include at least one option.";

        if (options.Any(o => string.IsNullOrWhiteSpace(o.Value)
                          || string.IsNullOrWhiteSpace(o.LabelAr)
                          || string.IsNullOrWhiteSpace(o.LabelEn)))
            return "Each option must have a non-empty value, labelAr, and labelEn.";

        var values = options.Select(o => o.Value.Trim()).ToList();
        if (values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Count)
            return "Option values must be unique within the requirement.";

        if (minCount < 1)
            return "Selection MinCount must be at least 1.";
        if (maxCount > options.Count)
            return $"MaxCount ({maxCount}) cannot exceed the number of options ({options.Count}).";

        return null;
    }
}
