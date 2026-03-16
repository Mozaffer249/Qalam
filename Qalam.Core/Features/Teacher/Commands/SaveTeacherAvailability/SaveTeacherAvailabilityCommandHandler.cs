using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Queries.GetTeacherAvailability;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.SaveTeacherAvailability;

public class SaveTeacherAvailabilityCommandHandler : ResponseHandler,
    IRequestHandler<SaveTeacherAvailabilityCommand, Response<TeacherAvailabilityResponseDto>>
{
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMediator _mediator;

    public SaveTeacherAvailabilityCommandHandler(
        ITeacherAvailabilityRepository availabilityRepository,
        ITeacherRepository teacherRepository,
        IMediator mediator,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _availabilityRepository = availabilityRepository;
        _teacherRepository = teacherRepository;
        _mediator = mediator;
    }

    public async Task<Response<TeacherAvailabilityResponseDto>> Handle(
        SaveTeacherAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        // Get teacher from UserId
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
        {
            return NotFound<TeacherAvailabilityResponseDto>("Teacher not found");
        }

        // Validate at least one day is provided
        if (request.DaySchedules.Count == 0)
        {
            return BadRequest<TeacherAvailabilityResponseDto>("At least one day schedule is required");
        }

        // Save availability (additive — skips existing slots automatically)
        await _availabilityRepository.SaveTeacherAvailabilityAsync(teacher.Id, request.DaySchedules);

        // Return updated availability using the query handler
        var query = new GetTeacherAvailabilityQuery { UserId = request.UserId };
        var result = await _mediator.Send(query, cancellationToken);

        return result.Succeeded
            ? Success("Teacher availability saved successfully", entity: result.Data)
            : result;
    }
}
