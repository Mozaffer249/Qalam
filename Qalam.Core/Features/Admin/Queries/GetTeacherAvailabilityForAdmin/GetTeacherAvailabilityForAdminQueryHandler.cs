using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetTeacherAvailabilityForAdmin;

public class GetTeacherAvailabilityForAdminQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherAvailabilityForAdminQuery, Response<TeacherAvailabilityResponseDto>>
{
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public GetTeacherAvailabilityForAdminQueryHandler(
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
        GetTeacherAvailabilityForAdminQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
        if (teacher == null)
            return NotFound<TeacherAvailabilityResponseDto>("Teacher not found");

        var availabilitySlots = await _availabilityRepository.GetTeacherAvailabilityAsync(teacher.Id);

        var fromDate = request.FromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var toDate = request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90));
        var exceptions = await _availabilityRepository.GetTeacherExceptionsAsync(teacher.Id, fromDate, toDate);

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
