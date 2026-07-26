using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Queries.GetTeacherRegistrationRequirements;

public class GetTeacherRegistrationRequirementsQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherRegistrationRequirementsQuery, Response<TeacherRegistrationRequirementsResponseDto>>
{
    private readonly ITeacherRegistrationRequirementProvider _provider;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherRegistrationStatusService _registrationStatusService;

    public GetTeacherRegistrationRequirementsQueryHandler(
        ITeacherRegistrationRequirementProvider provider,
        ITeacherRepository teacherRepository,
        ITeacherRegistrationStatusService registrationStatusService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
        _teacherRepository = teacherRepository;
        _registrationStatusService = registrationStatusService;
    }

    public async Task<Response<TeacherRegistrationRequirementsResponseDto>> Handle(
        GetTeacherRegistrationRequirementsQuery request,
        CancellationToken cancellationToken)
    {
        var requirements = await _provider.GetActivePublicDtosAsync(cancellationToken);

        if (request.UserId > 0)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
            if (teacher != null)
            {
                var checklist = await _registrationStatusService.GetChecklistForTeacherAsync(
                    teacher.Id, cancellationToken);
                var submitted = checklist
                    .Where(c => c.IsSubmitted)
                    .Select(c => c.Code)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var req in requirements)
                    req.IsSubmitted = submitted.Contains(req.Code);
            }
        }

        return Success(entity: new TeacherRegistrationRequirementsResponseDto { Requirements = requirements });
    }
}
