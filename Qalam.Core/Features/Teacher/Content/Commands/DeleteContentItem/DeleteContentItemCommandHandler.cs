using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.DeleteContentItem;

public class DeleteContentItemCommandHandler : ResponseHandler,
    IRequestHandler<DeleteContentItemCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public DeleteContentItemCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<string>> Handle(
        DeleteContentItemCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found");

        var ok = await _contentService.DeleteItemAsync(teacher.Id, request.Id, cancellationToken);
        if (!ok)
            return BadRequest<string>("Item not found or linked to sessions.");
        return Success(entity: "Deleted");
    }
}
