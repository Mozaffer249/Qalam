using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.CompletePersonalInfo;

public class CompletePersonalInfoCommandHandler : ResponseHandler,
    IRequestHandler<CompletePersonalInfoCommand, Response<TeacherAccountDto>>
{
    private readonly ITeacherRegistrationService _teacherRegistrationService;

    public CompletePersonalInfoCommandHandler(
        ITeacherRegistrationService teacherRegistrationService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRegistrationService = teacherRegistrationService;
    }

    public async Task<Response<TeacherAccountDto>> Handle(
        CompletePersonalInfoCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // UserId is automatically populated by UserIdentityBehavior
            if (request.UserId == 0)
            {
                return Unauthorized<TeacherAccountDto>("User not authenticated");
            }

            var result = await _teacherRegistrationService.CompleteAccountAsync(
                request.UserId,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password);

            return Success<TeacherAccountDto>(entity: result);
        }
        catch (Exception ex)
        {
            return BadRequest<TeacherAccountDto>(ex.Message);
        }
    }
}
