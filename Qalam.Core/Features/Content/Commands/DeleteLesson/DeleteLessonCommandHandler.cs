using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Commands.DeleteLesson;

public class DeleteLessonCommandHandler : ResponseHandler,
    IRequestHandler<DeleteLessonCommand, Response<bool>>
{
    private readonly IContentManagementService _contentService;

    public DeleteLessonCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<bool>> Handle(
        DeleteLessonCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _contentService.DeleteLessonAsync(request.Id);
            if (!result)
                return NotFound<bool>("Lesson not found");

            return Deleted<bool>();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
