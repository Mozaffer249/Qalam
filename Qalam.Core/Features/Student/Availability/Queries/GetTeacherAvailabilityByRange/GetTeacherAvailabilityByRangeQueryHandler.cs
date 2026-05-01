using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Availability.Queries.GetTeacherAvailabilityByRange;

public class GetTeacherAvailabilityByRangeQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherAvailabilityByRangeQuery, Response<TeacherAvailabilityByWeekdayRangeDto>>
{
    private const int DefaultRangeDays = 30;
    private const int MaxRangeDays = 90;

    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherAvailabilityCalendarService _calendarService;

    public GetTeacherAvailabilityByRangeQueryHandler(
        ITeacherRepository teacherRepository,
        ITeacherAvailabilityCalendarService calendarService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _calendarService = calendarService;
    }

    public async Task<Response<TeacherAvailabilityByWeekdayRangeDto>> Handle(
        GetTeacherAvailabilityByRangeQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
        if (teacher == null)
            return NotFound<TeacherAvailabilityByWeekdayRangeDto>("Teacher not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = request.FromDate ?? today;
        var toDate = request.ToDate ?? fromDate.AddDays(DefaultRangeDays);

        if (toDate < fromDate)
            return BadRequest<TeacherAvailabilityByWeekdayRangeDto>("ToDate must be on or after FromDate.");

        var maxAllowed = fromDate.AddDays(MaxRangeDays);
        if (toDate > maxAllowed)
            toDate = maxAllowed;

        var dto = await _calendarService.BuildWeekdayRangeDtoAsync(request.TeacherId, fromDate, toDate, cancellationToken);
        return Success(entity: dto);
    }
}
