using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teaching;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teaching;
using Qalam.Data.Entity.Common;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Commands.UpdateTimeSlot;

public class UpdateTimeSlotCommandHandler : ResponseHandler,
    IRequestHandler<UpdateTimeSlotCommand, Response<TimeSlotDto>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public UpdateTimeSlotCommandHandler(
        ITeachingConfigurationService teachingService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<TimeSlotDto>> Handle(
        UpdateTimeSlotCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Data;
            var entity = await _teachingService.UpdateTimeSlotAsync(new TimeSlot
            {
                Id = request.Id,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                DurationMinutes = dto.DurationMinutes,
                LabelAr = dto.LabelAr?.Trim(),
                LabelEn = dto.LabelEn?.Trim(),
                IsActive = dto.IsActive
            });

            return Success("Time slot updated", entity: TimeSlotMapper.ToDto(entity));
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound<TimeSlotDto>(ex.Message);
            return BadRequest<TimeSlotDto>(ex.Message);
        }
    }
}
