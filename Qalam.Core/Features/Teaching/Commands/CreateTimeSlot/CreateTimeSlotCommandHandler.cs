using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teaching;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teaching;
using Qalam.Data.Entity.Common;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Commands.CreateTimeSlot;

public class CreateTimeSlotCommandHandler : ResponseHandler,
    IRequestHandler<CreateTimeSlotCommand, Response<TimeSlotDto>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public CreateTimeSlotCommandHandler(
        ITeachingConfigurationService teachingService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<TimeSlotDto>> Handle(
        CreateTimeSlotCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Data;
            var entity = await _teachingService.CreateTimeSlotAsync(new TimeSlot
            {
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                DurationMinutes = dto.DurationMinutes,
                LabelAr = dto.LabelAr?.Trim(),
                LabelEn = dto.LabelEn?.Trim(),
                IsActive = dto.IsActive
            });

            return Success("Time slot created", entity: TimeSlotMapper.ToDto(entity));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<TimeSlotDto>(ex.Message);
        }
    }
}
