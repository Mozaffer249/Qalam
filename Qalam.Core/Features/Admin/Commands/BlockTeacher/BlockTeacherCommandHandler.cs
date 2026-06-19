using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.BlockTeacher;

public class BlockTeacherCommandHandler : ResponseHandler,
	IRequestHandler<BlockTeacherCommand, Response<string>>
{
	private readonly ITeacherManagementService _teacherManagementService;
	private readonly ILogger<BlockTeacherCommandHandler> _logger;

	public BlockTeacherCommandHandler(
		ITeacherManagementService teacherManagementService,
		ILogger<BlockTeacherCommandHandler> logger,
		IStringLocalizer<SharedResources> localizer) : base(localizer)
	{
		_teacherManagementService = teacherManagementService;
		_logger = logger;
	}

	public async Task<Response<string>> Handle(
		BlockTeacherCommand request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation(
				"Admin {AdminId} toggling block for teacher {TeacherId}{Reason}",
				request.UserId,
				request.TeacherId,
				string.IsNullOrEmpty(request.Reason) ? "" : $" with reason: {request.Reason}");

			var (success, _, message) = await _teacherManagementService.ToggleBlockTeacherAsync(
				request.TeacherId,
				request.UserId,
				request.Reason);

			if (!success)
				return NotFound<string>(message);

			return Success<string>(message);
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Error toggling block for teacher {TeacherId}",
				request.TeacherId);
			return BadRequest<string>("Failed to update teacher account block status");
		}
	}
}
