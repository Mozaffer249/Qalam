using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Commands.RestoreTeacherSubject;

public class RestoreTeacherSubjectCommandHandler : ResponseHandler,
    IRequestHandler<RestoreTeacherSubjectCommand, Response<string>>
{
    private readonly ITeacherSubjectAdminService _subjectAdminService;
    private readonly ILogger<RestoreTeacherSubjectCommandHandler> _logger;

    public RestoreTeacherSubjectCommandHandler(
        ITeacherSubjectAdminService subjectAdminService,
        ILogger<RestoreTeacherSubjectCommandHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _subjectAdminService = subjectAdminService;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(
        RestoreTeacherSubjectCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _subjectAdminService.RestoreSubjectAsync(
                request.TeacherId, request.TeacherSubjectId, request.UserId, cancellationToken);

            if (!success)
                return NotFound<string>("Teacher subject not found");

            return Success<string>("Teacher subject restored successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error restoring teacher subject {TeacherSubjectId} for teacher {TeacherId}",
                request.TeacherSubjectId, request.TeacherId);
            return BadRequest<string>("Failed to restore teacher subject");
        }
    }
}
