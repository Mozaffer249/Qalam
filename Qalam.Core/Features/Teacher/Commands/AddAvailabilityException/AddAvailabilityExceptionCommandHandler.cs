using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.AddAvailabilityException;

public class AddAvailabilityExceptionCommandHandler : ResponseHandler,
    IRequestHandler<AddAvailabilityExceptionCommand, Response<AvailabilityExceptionResponseDto>>
{
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public AddAvailabilityExceptionCommandHandler(
        ITeacherAvailabilityRepository availabilityRepository,
        ITeacherRepository teacherRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _availabilityRepository = availabilityRepository;
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<Response<AvailabilityExceptionResponseDto>> Handle(
        AddAvailabilityExceptionCommand request,
        CancellationToken cancellationToken)
    {
        // Get teacher from UserId
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
        {
            return NotFound<AvailabilityExceptionResponseDto>("Teacher not found");
        }

        // Check if exception already exists
        var exists = await _availabilityRepository.ExceptionExistsAsync(
            teacher.Id,
            request.Date,
            request.TimeSlotId);

        if (exists)
        {
            return BadRequest<AvailabilityExceptionResponseDto>(
                "An exception already exists for this date and time slot");
        }

        // Create DTO
        var dto = new AddAvailabilityExceptionDto
        {
            Date = request.Date,
            TimeSlotId = request.TimeSlotId,
            ExceptionType = request.ExceptionType,
            Reason = request.Reason
        };

        // Add exception
        var exception = await _availabilityRepository.AddExceptionAsync(teacher.Id, dto);

        if (exception == null)
        {
            return BadRequest<AvailabilityExceptionResponseDto>("Failed to add exception");
        }

        // Map to response DTO
        var response = _mapper.Map<AvailabilityExceptionResponseDto>(exception);

        return Success("Availability exception added successfully", entity: response);
    }
}
