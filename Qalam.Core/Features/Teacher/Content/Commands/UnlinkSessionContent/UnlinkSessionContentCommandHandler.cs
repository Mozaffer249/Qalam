using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Teacher.Content.Commands.UnlinkSessionContent;

public class UnlinkSessionContentCommandHandler : ResponseHandler,
    IRequestHandler<UnlinkSessionContentCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherContentService _contentService;

    public UnlinkSessionContentCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        ITeacherContentService contentService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _contentService = contentService;
    }

    public async Task<Response<string>> Handle(
        UnlinkSessionContentCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found");

        var ok = await _contentService.UnlinkSessionContentAsync(
            teacher.Id, request.ScheduleId, request.LinkId, cancellationToken);
        if (!ok)
            return NotFound<string>("Link not found.");
        return Success(entity: "Unlinked");
    }
}
