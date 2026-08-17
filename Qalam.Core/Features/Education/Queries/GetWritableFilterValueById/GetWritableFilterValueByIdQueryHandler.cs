using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetWritableFilterValueById;

public class GetWritableFilterValueByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetWritableFilterValueByIdQuery, Response<WritableFilterValueDto>>
{
    private readonly IWritableFilterRepository _writableFilterRepository;

    public GetWritableFilterValueByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IWritableFilterRepository writableFilterRepository) : base(localizer)
    {
        _writableFilterRepository = writableFilterRepository;
    }

    public async Task<Response<WritableFilterValueDto>> Handle(
        GetWritableFilterValueByIdQuery request,
        CancellationToken cancellationToken)
    {
        var value = await _writableFilterRepository.GetByIdWithSlotAsync(request.Id, cancellationToken);
        if (value == null)
            return NotFound<WritableFilterValueDto>("Writable filter value not found");

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
