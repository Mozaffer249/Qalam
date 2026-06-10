using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Commands.ActivateTeacherSubject;

public class ActivateTeacherSubjectCommandHandler : ResponseHandler,
    IRequestHandler<ActivateTeacherSubjectCommand, Response<string>>
{
    private readonly ITeacherSubjectAdminService _subjectAdminService;
    private readonly ILogger<ActivateTeacherSubjectCommandHandler> _logger;

    public ActivateTeacherSubjectCommandHandler(
        ITeacherSubjectAdminService subjectAdminService,
        ILogger<ActivateTeacherSubjectCommandHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _subjectAdminService = subjectAdminService;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(
        ActivateTeacherSubjectCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _subjectAdminService.ActivateSubjectAsync(
                request.TeacherId, request.TeacherSubjectId, request.UserId, cancellationToken);

            if (!success)
                return NotFound<string>("Teacher subject not found");

            return Success<string>("Teacher subject activated successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error activating teacher subject {TeacherSubjectId} for teacher {TeacherId}",
                request.TeacherSubjectId, request.TeacherId);
            return BadRequest<string>("Failed to activate teacher subject");
        }
    }
}
