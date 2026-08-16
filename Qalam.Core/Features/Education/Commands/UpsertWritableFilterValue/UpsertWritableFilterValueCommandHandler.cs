using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Commands.UpsertWritableFilterValue;

public class UpsertWritableFilterValueCommandHandler : ResponseHandler,
    IRequestHandler<UpsertWritableFilterValueCommand, Response<WritableFilterValueDto>>
{
    private readonly IWritableFilterRepository _writableFilterRepository;
    private readonly IEducationDomainRepository _domainRepository;

    public UpsertWritableFilterValueCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IWritableFilterRepository writableFilterRepository,
        IEducationDomainRepository domainRepository) : base(localizer)
    {
        _writableFilterRepository = writableFilterRepository;
        _domainRepository = domainRepository;
    }

    public async Task<Response<WritableFilterValueDto>> Handle(
        UpsertWritableFilterValueCommand request,
        CancellationToken cancellationToken)
    {
        var domain = await _domainRepository.GetByIdAsync(request.DomainId);
        if (domain == null || !domain.IsActive)
            return NotFound<WritableFilterValueDto>("Education domain not found");

        var slot = await _writableFilterRepository.GetSlotByDomainAndCodeAsync(
            request.DomainId,
            request.SlotCode,
            cancellationToken);
        if (slot == null)
            return BadRequest<WritableFilterValueDto>($"Unknown writable slot '{request.SlotCode}'");

        var normalized = WritableFilterTextNormalizer.Normalize(request.Text);
        if (string.IsNullOrEmpty(normalized))
            return BadRequest<WritableFilterValueDto>("Text is required");

        var existing = await _writableFilterRepository.FindByNormalizedAsync(slot.Id, normalized, cancellationToken);
        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                await _writableFilterRepository.UpdateAsync(existing);
            }

            return Success(entity: Map(existing, slot.Code));
        }

        var created = new WritableFilterValue
        {
            SlotId = slot.Id,
            NameAr = request.Text.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? request.Text.Trim() : request.NameEn.Trim(),
            NormalizedText = normalized,
            IsSeeded = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _writableFilterRepository.AddAsync(created);
        return Created(entity: Map(created, slot.Code));
    }

    private static WritableFilterValueDto Map(WritableFilterValue value, string slotCode) => new()
    {
        Id = value.Id,
        SlotId = value.SlotId,
        SlotCode = slotCode,
        Code = value.Code,
        NameAr = value.NameAr,
        NameEn = value.NameEn,
        IsSeeded = value.IsSeeded
    };
}
