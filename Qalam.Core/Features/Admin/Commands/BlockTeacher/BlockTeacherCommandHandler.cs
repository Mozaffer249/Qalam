using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.BlockTeacher;

public class BlockTeacherCommandHandler : ResponseHandler,
	IRequestHandler<BlockTeacherCommand, Response<string>>
{
	private readonly ITeacherRepository _teacherRepository;
	private readonly ILogger<BlockTeacherCommandHandler> _logger;

	public BlockTeacherCommandHandler(
		ITeacherRepository teacherRepository,
		ILogger<BlockTeacherCommandHandler> logger,
		IStringLocalizer<SharedResources> localizer) : base(localizer)
	{
		_teacherRepository = teacherRepository;
		_logger = logger;
	}

	public async Task<Response<string>> Handle(
		BlockTeacherCommand request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation(
				"Admin {AdminId} attempting to block teacher {TeacherId}{Reason}",
				request.UserId,
				request.TeacherId,
				string.IsNullOrEmpty(request.Reason) ? "" : $" with reason: {request.Reason}");

			// Get the teacher
			var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
			if (teacher == null)
			{
				_logger.LogWarning("Teacher {TeacherId} not found", request.TeacherId);
				return NotFound<string>("Teacher not found");
			}

			// Update teacher status to blocked
			teacher.Status = TeacherStatus.Blocked;
			teacher.IsActive = false;

			await _teacherRepository.UpdateAsync(teacher);
			await _teacherRepository.SaveChangesAsync();

			_logger.LogInformation(
				"Teacher {TeacherId} blocked by admin {AdminId}{Reason}",
				request.TeacherId,
				request.UserId,
				string.IsNullOrEmpty(request.Reason) ? "" : $": {request.Reason}");

			return Success<string>("Teacher account blocked successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Error blocking teacher {TeacherId}",
				request.TeacherId);
			return BadRequest<string>("Failed to block teacher account");
		}
	}
}
