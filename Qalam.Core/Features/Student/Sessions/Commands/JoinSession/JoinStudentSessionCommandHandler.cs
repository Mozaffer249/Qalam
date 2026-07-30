using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Sessions.Commands.JoinSession;

public class JoinStudentSessionCommandHandler : ResponseHandler,
    IRequestHandler<JoinStudentSessionCommand, Response<StudentSessionJoinDto>>
{
    private readonly ISessionPresenceService _presenceService;
    private readonly ICourseScheduleRepository _scheduleRepository;
    private readonly LiveSessionSettings _liveSessionSettings;

    public JoinStudentSessionCommandHandler(
        ISessionPresenceService presenceService,
        ICourseScheduleRepository scheduleRepository,
        IOptions<LiveSessionSettings> liveSessionSettings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _presenceService = presenceService;
        _scheduleRepository = scheduleRepository;
        _liveSessionSettings = liveSessionSettings.Value;
    }

    public async Task<Response<StudentSessionJoinDto>> Handle(
        JoinStudentSessionCommand request,
        CancellationToken cancellationToken)
    {
        var (ok, message, forbidden, notFound) = await _presenceService.JoinAsStudentAsync(
            request.UserId, request.Id, cancellationToken);

        if (forbidden) return Forbidden<StudentSessionJoinDto>(message);
        if (notFound) return NotFound<StudentSessionJoinDto>(message);
        if (!ok) return BadRequest<StudentSessionJoinDto>(message);

        var meetingUrl = await ResolveMeetingUrlAsync(request.Id, cancellationToken);
        return Success(entity: new StudentSessionJoinDto
        {
            Message = string.IsNullOrWhiteSpace(message) ? "Joined." : message,
            MeetingUrl = meetingUrl,
        });
    }

    private async Task<string?> ResolveMeetingUrlAsync(int scheduleId, CancellationToken ct)
    {
        var schedule = await _scheduleRepository.GetTableNoTracking()
            .Include(cs => cs.TeachingMode)
            .FirstOrDefaultAsync(cs => cs.Id == scheduleId, ct);

        var isOnline = string.Equals(
            schedule?.TeachingMode?.Code, "online", StringComparison.OrdinalIgnoreCase);
        if (!isOnline)
            return null;

        var url = _liveSessionSettings.LiveKit?.Url?.Trim();
        return string.IsNullOrWhiteSpace(url) ? null : url;
    }
}
