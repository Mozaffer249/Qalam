using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.CompletePersonalInfo;

public class CompletePersonalInfoCommandHandler : ResponseHandler,
    IRequestHandler<CompletePersonalInfoCommand, Response<TeacherAccountResponseDto>>
{
    private readonly ITeacherRegistrationService _teacherRegistrationService;

    public CompletePersonalInfoCommandHandler(
        ITeacherRegistrationService teacherRegistrationService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRegistrationService = teacherRegistrationService;
    }

    public async Task<Response<TeacherAccountResponseDto>> Handle(
        CompletePersonalInfoCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // UserId is automatically populated by UserIdentityBehavior
            if (request.UserId == 0)
            {
                return Unauthorized<TeacherAccountResponseDto>("User not authenticated");
            }

            var account = await _teacherRegistrationService.CompleteAccountAsync(
                request.UserId,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password);

            // Get next step (should be Step 4 - Upload Documents)
            var nextStep = await _teacherRegistrationService
                .GetNextRegistrationStepAsync(request.UserId);

            return Success(entity: new TeacherAccountResponseDto
            {
                Account = account,
                NextStep = nextStep
            });
        }
        catch (Exception ex)
        {
            return BadRequest<TeacherAccountResponseDto>(ex.Message);
        }
    }
}
