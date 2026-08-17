using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Commands.UpdateWritableFilterValue;

public class UpdateWritableFilterValueCommandHandler : ResponseHandler,
    IRequestHandler<UpdateWritableFilterValueCommand, Response<WritableFilterValueDto>>
{
    private readonly IWritableFilterRepository _writableFilterRepository;

    public UpdateWritableFilterValueCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IWritableFilterRepository writableFilterRepository) : base(localizer)
    {
        _writableFilterRepository = writableFilterRepository;
    }

    public async Task<Response<WritableFilterValueDto>> Handle(
        UpdateWritableFilterValueCommand request,
        CancellationToken cancellationToken)
    {
        var value = await _writableFilterRepository.GetByIdWithSlotAsync(request.Id, cancellationToken);
        if (value == null)
            return NotFound<WritableFilterValueDto>("Writable filter value not found");

        var nameAr = request.NameAr.Trim();
        var nameEn = request.NameEn.Trim();
        var normalized = WritableFilterTextNormalizer.Normalize(nameAr);
        if (string.IsNullOrEmpty(normalized))
            return BadRequest<WritableFilterValueDto>("NameAr is required");

        var clash = await _writableFilterRepository.FindByNormalizedAsync(value.SlotId, normalized, cancellationToken);
        if (clash != null && clash.Id != value.Id)
            return BadRequest<WritableFilterValueDto>("Another value with the same text already exists in this slot");

        value.NameAr = nameAr;
        value.NameEn = nameEn;
        value.NormalizedText = normalized;
        value.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        value.SubjectCodeContains = string.IsNullOrWhiteSpace(request.SubjectCodeContains)
            ? null
            : request.SubjectCodeContains.Trim();
        value.IsActive = request.IsActive;
        value.UpdatedAt = DateTime.UtcNow;

        await _writableFilterRepository.UpdateAsync(value);

        return Success(entity: new WritableFilterValueDto
        {
            Id = value.Id,
            SlotId = value.SlotId,
            SlotCode = value.Slot?.Code ?? string.Empty,
            Code = value.Code,
            NameAr = value.NameAr,
            NameEn = value.NameEn,
            IsSeeded = value.IsSeeded,
            IsActive = value.IsActive,
            SubjectCodeContains = value.SubjectCodeContains
        });
    }
}
