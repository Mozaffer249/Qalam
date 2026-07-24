using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Sessions;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Sessions.Queries.GetSessionReviews;

public class GetSessionReviewsQueryHandler : ResponseHandler,
    IRequestHandler<GetSessionReviewsQuery, Response<List<SessionReviewDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly ISessionReviewService _reviewService;

    public GetSessionReviewsQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseScheduleRepository scheduleRepository,
        ISessionReviewService reviewService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _scheduleRepository = scheduleRepository;
        _reviewService = reviewService;
    }

    public async Task<Response<List<SessionReviewDto>>> Handle(
        GetSessionReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<List<SessionReviewDto>>("Teacher profile not found.");

        var schedule = await _scheduleRepository.GetByIdForLifecycleAsync(request.Id, cancellationToken);
        if (schedule == null)
            return NotFound<List<SessionReviewDto>>("Session not found.");

        if (!TeacherSessionCommandHelpers.TeacherOwnsSchedule(schedule, teacher.Id))
            return Forbidden<List<SessionReviewDto>>("This session does not belong to you.");

        var reviews = await _reviewService.GetReviewsForSessionAsync(request.Id, cancellationToken);
        return Success(entity: reviews);
    }
}
