using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Curriculum.Commands.ToggleCurriculumStatus;

public class ToggleCurriculumStatusCommandHandler : ResponseHandler,
    IRequestHandler<ToggleCurriculumStatusCommand, Response<bool>>
{
    private readonly ICurriculumService _curriculumService;

    public ToggleCurriculumStatusCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ICurriculumService curriculumService) : base(localizer)
    {
        _curriculumService = curriculumService;
    }

    public async Task<Response<bool>> Handle(
        ToggleCurriculumStatusCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _curriculumService.ToggleCurriculumStatusAsync(request.Id);
        
        if (!result)
            return NotFound<bool>("Curriculum not found");

        return Success(entity: true, Message: "Curriculum status toggled successfully");
    }
}
