using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.DeleteEducationLevel;

public class DeleteEducationLevelCommandHandler : ResponseHandler,
    IRequestHandler<DeleteEducationLevelCommand, Response<bool>>
{
    private readonly IGradeService _gradeService;

    public DeleteEducationLevelCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<bool>> Handle(
        DeleteEducationLevelCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeService.DeleteLevelAsync(request.Id);
            if (!result)
                return NotFound<bool>("Education level not found");

            return Deleted<bool>();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
