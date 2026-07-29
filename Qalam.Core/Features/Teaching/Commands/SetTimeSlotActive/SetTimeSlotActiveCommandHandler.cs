using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teaching;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teaching;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Commands.SetTimeSlotActive;

public class SetTimeSlotActiveCommandHandler : ResponseHandler,
    IRequestHandler<SetTimeSlotActiveCommand, Response<TimeSlotDto>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public SetTimeSlotActiveCommandHandler(
        ITeachingConfigurationService teachingService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<TimeSlotDto>> Handle(
        SetTimeSlotActiveCommand request,
        CancellationToken cancellationToken)
    {
        var ok = await _teachingService.SetTimeSlotActiveAsync(request.Id, request.Data.IsActive);
        if (!ok)
            return NotFound<TimeSlotDto>("Time slot not found");

        var entity = await _teachingService.GetTimeSlotByIdAsync(request.Id);
        if (entity == null)
            return NotFound<TimeSlotDto>("Time slot not found");

        return Success(entity: TimeSlotMapper.ToDto(entity));
    }
}
