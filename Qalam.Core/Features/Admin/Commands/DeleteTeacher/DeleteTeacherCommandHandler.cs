using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.DeleteTeacher;

public class DeleteTeacherCommandHandler : ResponseHandler,
    IRequestHandler<DeleteTeacherCommand, Response<string>>
{
    private readonly ITeacherManagementService _teacherManagementService;
    private readonly ILogger<DeleteTeacherCommandHandler> _logger;

    public DeleteTeacherCommandHandler(
        ITeacherManagementService teacherManagementService,
        ILogger<DeleteTeacherCommandHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherManagementService = teacherManagementService;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(
        DeleteTeacherCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Admin {AdminId} deleting teacher {TeacherId}{Reason}",
                request.UserId,
                request.TeacherId,
                string.IsNullOrEmpty(request.Reason) ? "" : $" with reason: {request.Reason}");

            var (success, message) = await _teacherManagementService.DeleteTeacherAccountAsync(
                request.TeacherId,
                request.UserId,
                request.Reason,
                cancellationToken);

            if (!success)
            {
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound<string>(message);
                return BadRequest<string>(message);
            }

            return Success<string>(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting teacher {TeacherId}", request.TeacherId);
            return BadRequest<string>("Failed to delete teacher account");
        }
    }
}
