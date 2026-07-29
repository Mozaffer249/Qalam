using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teaching.Commands.DeleteTimeSlot;

public class DeleteTimeSlotCommandHandler : ResponseHandler,
    IRequestHandler<DeleteTimeSlotCommand, Response<string>>
{
    private readonly ITeachingConfigurationService _teachingService;

    public DeleteTimeSlotCommandHandler(
        ITeachingConfigurationService teachingService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teachingService = teachingService;
    }

    public async Task<Response<string>> Handle(
        DeleteTimeSlotCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _teachingService.DeleteTimeSlotAsync(request.Id);
            if (!deleted)
                return NotFound<string>("Time slot not found");

            return Success("Time slot deleted", entity: "Deleted");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }
}
