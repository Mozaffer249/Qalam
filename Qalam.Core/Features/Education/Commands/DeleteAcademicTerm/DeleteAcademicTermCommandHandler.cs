using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.DeleteAcademicTerm;

public class DeleteAcademicTermCommandHandler : ResponseHandler,
    IRequestHandler<DeleteAcademicTermCommand, Response<bool>>
{
    private readonly IGradeService _gradeService;

    public DeleteAcademicTermCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IGradeService gradeService) : base(localizer)
    {
        _gradeService = gradeService;
    }

    public async Task<Response<bool>> Handle(
        DeleteAcademicTermCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _gradeService.DeleteTermAsync(request.Id);
            if (!result)
                return NotFound<bool>("Academic term not found");

            return Deleted<bool>();
        }
        catch (DbUpdateException)
        {
            return BadRequest<bool>("Cannot delete term: related data still references it.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
