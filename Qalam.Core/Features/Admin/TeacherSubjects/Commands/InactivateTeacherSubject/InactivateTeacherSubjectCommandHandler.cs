using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Commands.InactivateTeacherSubject;

public class InactivateTeacherSubjectCommandHandler : ResponseHandler,
    IRequestHandler<InactivateTeacherSubjectCommand, Response<string>>
{
    private readonly ITeacherSubjectAdminService _subjectAdminService;
    private readonly ILogger<InactivateTeacherSubjectCommandHandler> _logger;

    public InactivateTeacherSubjectCommandHandler(
        ITeacherSubjectAdminService subjectAdminService,
        ILogger<InactivateTeacherSubjectCommandHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _subjectAdminService = subjectAdminService;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(
        InactivateTeacherSubjectCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _subjectAdminService.InactivateSubjectAsync(
                request.TeacherId, request.TeacherSubjectId, request.UserId, cancellationToken);

            if (!success)
                return NotFound<string>("Teacher subject not found");

            return Success<string>("Teacher subject inactivated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inactivating teacher subject {TeacherSubjectId} for teacher {TeacherId}",
                request.TeacherSubjectId, request.TeacherId);
            return BadRequest<string>("Failed to inactivate teacher subject");
        }
    }
}
