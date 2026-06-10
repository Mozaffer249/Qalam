using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Commands.RejectTeacherSubject;

public class RejectTeacherSubjectCommandHandler : ResponseHandler,
    IRequestHandler<RejectTeacherSubjectCommand, Response<string>>
{
    private readonly ITeacherSubjectAdminService _subjectAdminService;
    private readonly ILogger<RejectTeacherSubjectCommandHandler> _logger;

    public RejectTeacherSubjectCommandHandler(
        ITeacherSubjectAdminService subjectAdminService,
        ILogger<RejectTeacherSubjectCommandHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _subjectAdminService = subjectAdminService;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(
        RejectTeacherSubjectCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest<string>("Rejection reason is required.");

            var success = await _subjectAdminService.RejectSubjectAsync(
                request.TeacherId,
                request.TeacherSubjectId,
                request.UserId,
                request.Reason.Trim(),
                cancellationToken);

            if (!success)
                return NotFound<string>("Teacher subject not found");

            return Success<string>("Teacher subject rejected successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error rejecting teacher subject {TeacherSubjectId} for teacher {TeacherId}",
                request.TeacherSubjectId, request.TeacherId);
            return BadRequest<string>("Failed to reject teacher subject");
        }
    }
}
