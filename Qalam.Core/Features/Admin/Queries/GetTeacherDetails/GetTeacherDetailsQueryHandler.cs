using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetTeacherDetails;

public class GetTeacherDetailsQueryHandler : ResponseHandler,
	IRequestHandler<GetTeacherDetailsQuery, Response<TeacherDetailsDto?>>
{
	private readonly ITeacherRepository _teacherRepository;
	private readonly ITeacherRegistrationStatusService _registrationStatusService;
	private readonly ILogger<GetTeacherDetailsQueryHandler> _logger;

	public GetTeacherDetailsQueryHandler(
		ITeacherRepository teacherRepository,
		ITeacherRegistrationStatusService registrationStatusService,
		ILogger<GetTeacherDetailsQueryHandler> logger,
		IStringLocalizer<SharedResources> localizer) : base(localizer)
	{
		_teacherRepository = teacherRepository;
		_registrationStatusService = registrationStatusService;
		_logger = logger;
	}

	public async Task<Response<TeacherDetailsDto?>> Handle(
		GetTeacherDetailsQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Fetching teacher details for TeacherId: {TeacherId}", request.TeacherId);

			var teacherDetails = await _teacherRepository.GetTeacherDetailsAsync(request.TeacherId);

			if (teacherDetails == null)
			{
				_logger.LogWarning("Teacher with ID {TeacherId} not found", request.TeacherId);
				return NotFound<TeacherDetailsDto?>("Teacher not found");
			}

			teacherDetails.RegistrationRequirements =
				await _registrationStatusService.GetChecklistForTeacherAsync(request.TeacherId, cancellationToken);

			var requiredItems = teacherDetails.RegistrationRequirements.Where(r => r.IsRequired).ToList();
			if (requiredItems.Count > 0)
			{
				teacherDetails.CanBeActivated = requiredItems.All(r =>
					r.IsSubmitted && r.VerificationStatus == DocumentVerificationStatus.Approved);
			}
			else
			{
				teacherDetails.CanBeActivated = teacherDetails.PendingDocuments == 0
					&& teacherDetails.RejectedDocuments == 0
					&& teacherDetails.TotalDocuments > 0;
			}

			_logger.LogInformation(
				"Successfully fetched details for teacher {TeacherId} with {DocumentCount} documents",
				request.TeacherId,
				teacherDetails.TotalDocuments);

			return Success<TeacherDetailsDto?>(entity: teacherDetails);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error fetching teacher details for TeacherId: {TeacherId}", request.TeacherId);
			return BadRequest<TeacherDetailsDto?>("Error retrieving teacher details");
		}
	}
}
