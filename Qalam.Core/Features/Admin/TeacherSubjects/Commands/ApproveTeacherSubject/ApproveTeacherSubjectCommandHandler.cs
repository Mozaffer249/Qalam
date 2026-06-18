using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Commands.ApproveTeacherSubject;

public class ApproveTeacherSubjectCommandHandler : ResponseHandler,
    IRequestHandler<ApproveTeacherSubjectCommand, Response<string>>
{
    private readonly ITeacherSubjectAdminService _subjectAdminService;
    private readonly ILogger<ApproveTeacherSubjectCommandHandler> _logger;

    public ApproveTeacherSubjectCommandHandler(
        ITeacherSubjectAdminService subjectAdminService,
        ILogger<ApproveTeacherSubjectCommandHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _subjectAdminService = subjectAdminService;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(
        ApproveTeacherSubjectCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _subjectAdminService.ApproveSubjectAsync(
                request.TeacherId, request.TeacherSubjectId, request.UserId, cancellationToken);

            if (!success)
                return NotFound<string>("Teacher subject not found");

            return Success<string>("Teacher subject approved successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error approving teacher subject {TeacherSubjectId} for teacher {TeacherId}",
                request.TeacherSubjectId, request.TeacherId);
            return BadRequest<string>("Failed to approve teacher subject");
        }
    }
}
