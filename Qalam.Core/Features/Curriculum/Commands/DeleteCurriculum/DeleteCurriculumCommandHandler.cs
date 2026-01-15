using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Curriculum.Commands.DeleteCurriculum;

public class DeleteCurriculumCommandHandler : ResponseHandler,
    IRequestHandler<DeleteCurriculumCommand, Response<bool>>
{
    private readonly ICurriculumService _curriculumService;

    public DeleteCurriculumCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ICurriculumService curriculumService) : base(localizer)
    {
        _curriculumService = curriculumService;
    }

    public async Task<Response<bool>> Handle(
        DeleteCurriculumCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _curriculumService.DeleteCurriculumAsync(request.Id);
            if (!result)
                return NotFound<bool>("Curriculum not found");

            return Deleted<bool>();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
