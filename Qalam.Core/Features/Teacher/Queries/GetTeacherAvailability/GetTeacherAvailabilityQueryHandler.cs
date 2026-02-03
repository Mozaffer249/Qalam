using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherAvailability;

public class GetTeacherAvailabilityQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherAvailabilityQuery, Response<TeacherAvailabilityResponseDto>>
{
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public GetTeacherAvailabilityQueryHandler(
        ITeacherAvailabilityRepository availabilityRepository,
        ITeacherRepository teacherRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _availabilityRepository = availabilityRepository;
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<Response<TeacherAvailabilityResponseDto>> Handle(
        GetTeacherAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        // Get teacher from UserId
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
        {
            return NotFound<TeacherAvailabilityResponseDto>("Teacher not found");
        }

        // Get weekly availability
        var availabilitySlots = await _availabilityRepository.GetTeacherAvailabilityAsync(teacher.Id);

        // Get exceptions (default: from today to 90 days ahead)
        var fromDate = request.FromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var toDate = request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90));
        var exceptions = await _availabilityRepository.GetTeacherExceptionsAsync(teacher.Id, fromDate, toDate);

        // Group availability by day
        var weeklySchedule = availabilitySlots
            .GroupBy(a => new
            {
                a.DayOfWeekId,
                a.DayOfWeek.NameAr,
                a.DayOfWeek.NameEn,
                a.DayOfWeek.OrderIndex
            })
            .OrderBy(g => g.Key.OrderIndex)
            .Select(g => new DayScheduleResponseDto
            {
                DayOfWeekId = g.Key.DayOfWeekId,
                DayNameAr = g.Key.NameAr,
                DayNameEn = g.Key.NameEn,
                TimeSlots = _mapper.Map<List<TimeSlotResponseDto>>(g.OrderBy(a => a.TimeSlot.StartTime).ToList())
            })
            .ToList();

        // Map exceptions
        var exceptionDtos = _mapper.Map<List<AvailabilityExceptionResponseDto>>(exceptions);

        var response = new TeacherAvailabilityResponseDto
        {
            TeacherId = teacher.Id,
            WeeklySchedule = weeklySchedule,
            Exceptions = exceptionDtos
        };

        return Success(entity: response);
    }
}
