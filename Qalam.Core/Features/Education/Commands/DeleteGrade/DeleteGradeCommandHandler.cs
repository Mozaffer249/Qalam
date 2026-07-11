using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.DeleteGrade;

public class DeleteGradeCommandHandler : ResponseHandler,
    IRequestHandler<DeleteGradeCommand, Response<bool>>
{
    private readonly IGradeService _gradeService;

    public DeleteGradeCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<bool>> Handle(
        DeleteGradeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeService.DeleteGradeAsync(request.Id);
            if (!result)
                return NotFound<bool>("Grade not found");

            return Deleted<bool>();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
