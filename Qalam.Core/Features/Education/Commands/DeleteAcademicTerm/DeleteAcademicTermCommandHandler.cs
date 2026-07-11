using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.DeleteAcademicTerm;

public class DeleteAcademicTermCommandHandler : ResponseHandler,
    IRequestHandler<DeleteAcademicTermCommand, Response<bool>>
{
    private readonly IGradeService _gradeService;
    private readonly IEducationDeleteGuardService _deleteGuard;

    public DeleteAcademicTermCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService,
        IEducationDeleteGuardService deleteGuard) : base(localizer)
    {
        _gradeService = gradeService;
        _deleteGuard = deleteGuard;
    }

    public async Task<Response<bool>> Handle(
        DeleteAcademicTermCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _deleteGuard.AssertCanDeleteTermAsync(request.Id);

            var result = await _gradeService.DeleteTermAsync(request.Id);
            if (!result)
                return NotFound<bool>("Academic term not found");

            return Deleted<bool>();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
