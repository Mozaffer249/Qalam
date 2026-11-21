using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Queries.GetTeacherAvailability;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.SaveTeacherAvailability;

public class SaveTeacherAvailabilityCommandHandler : ResponseHandler,
    IRequestHandler<SaveTeacherAvailabilityCommand, Response<TeacherAvailabilityResponseDto>>
{
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITimeSlotRepository _timeSlotRepository;
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly IMediator _mediator;

    public SaveTeacherAvailabilityCommandHandler(
        ITeacherAvailabilityRepository availabilityRepository,
        ITeacherRepository teacherRepository,
        ITimeSlotRepository timeSlotRepository,
        ITeacherRegistrationCompletionService completionService,
        IMediator mediator,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _availabilityRepository = availabilityRepository;
        _teacherRepository = teacherRepository;
        _timeSlotRepository = timeSlotRepository;
        _completionService = completionService;
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

        if (await _completionService.HasPendingRequiredRegistrationReviewAsync(teacher.Id, cancellationToken))
        {
            return BadRequest<TeacherAvailabilityResponseDto>(
                "Identity and certificate documents are still under review. Please wait for admin approval before setting availability.");
        }

        var overlap = await FindOverlapAsync(request.DaySchedules, cancellationToken);
        if (overlap != null)
        {
            return BadRequest<TeacherAvailabilityResponseDto>(overlap);
        }

        // Save availability (replace per submitted day — removes unchecked slots for those days)
        await _availabilityRepository.SaveTeacherAvailabilityAsync(teacher.Id, request.DaySchedules);

        // Return updated availability using the query handler
        var query = new GetTeacherAvailabilityQuery { UserId = request.UserId };
        var result = await _mediator.Send(query, cancellationToken);

        return result.Succeeded
            ? Success("Teacher availability saved successfully", entity: result.Data)
            : result;
    }

    /// <summary>
    /// Ensures no two slots picked for the same day overlap in real time.
    /// Needed since the seeder now emits both 60-min and 30-min TimeSlots and
    /// picking "9:00–10:00" alongside "9:00–9:30" would otherwise let two
    /// sessions get booked into the same half-hour on the same TeacherAvailability row.
    /// Returns an error message when a conflict is found, null otherwise.
    /// </summary>
    private async Task<string?> FindOverlapAsync(
        List<DayAvailabilityDto> daySchedules,
        CancellationToken cancellationToken)
    {
        var allIds = daySchedules
            .SelectMany(d => d.TimeSlotIds)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (allIds.Count == 0) return null;

        var slots = await _timeSlotRepository
            .GetTimeSlotsQueryable()
            .Where(ts => allIds.Contains(ts.Id))
            .ToListAsync(cancellationToken);

        return FindOverlap(daySchedules, slots);
    }

    /// <summary>Pure overlap check against a materialized TimeSlot catalog. Exposed so unit tests can hit it without a DB.</summary>
    public static string? FindOverlap(
        IEnumerable<DayAvailabilityDto> daySchedules,
        IReadOnlyCollection<TimeSlot> catalog)
    {
        var slotMap = catalog.ToDictionary(s => s.Id);

        foreach (var day in daySchedules)
        {
            var ordered = day.TimeSlotIds
                .Where(slotMap.ContainsKey)
                .Select(id => slotMap[id])
                .OrderBy(s => s.StartTime)
                .ToList();

            for (int i = 0; i + 1 < ordered.Count; i++)
            {
                var current = ordered[i];
                var next = ordered[i + 1];
                // Sorted by StartTime, so overlap iff current.End > next.Start.
                if (current.EndTime > next.StartTime)
                {
                    var dayName = DayNameAr(day.DayOfWeekId);
                    return $"يوم {dayName}: لا يمكن اختيار فترتين متداخلتين — {Format(current)} و {Format(next)}.";
                }
            }
        }

        return null;
    }

    private static string Format(TimeSlot s) =>
        $"{s.StartTime:hh\\:mm}–{s.EndTime:hh\\:mm}";

    /// <summary>
    /// Arabic day name for <c>DayOfWeekMaster.Id</c> (1=Sunday … 7=Saturday)
    /// as seeded in <c>DayOfWeekSeeder</c>. Avoids a DB round-trip inside the
    /// overlap validator and keeps the error message legible for the client.
    /// </summary>
    private static string DayNameAr(int dayOfWeekId) => dayOfWeekId switch
    {
        1 => "الأحد",
        2 => "الإثنين",
        3 => "الثلاثاء",
        4 => "الأربعاء",
        5 => "الخميس",
        6 => "الجمعة",
        7 => "السبت",
        _ => $"#{dayOfWeekId}",
    };
}
