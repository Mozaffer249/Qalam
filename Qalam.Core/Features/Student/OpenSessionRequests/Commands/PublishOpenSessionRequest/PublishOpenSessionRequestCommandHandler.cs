using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.PublishOpenSessionRequest;

public class PublishOpenSessionRequestCommandHandler
    : ResponseHandler, IRequestHandler<PublishOpenSessionRequestCommand, Response<OpenSessionRequestDetailDto>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IOpenSessionRequestTargetingService _targetingService;
    private readonly ITargetedOpenSessionRequestValidator _targetedValidator;
    private readonly IMapper _mapper;

    public PublishOpenSessionRequestCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IOpenSessionRequestTargetingService targetingService,
        ITargetedOpenSessionRequestValidator targetedValidator,
        IMapper mapper) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _targetingService = targetingService;
        _targetedValidator = targetedValidator;
        _mapper = mapper;
    }

    public async Task<Response<OpenSessionRequestDetailDto>> Handle(
        PublishOpenSessionRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.OpenSessionRequests
            .Include(r => r.Sessions).ThenInclude(s => s.Units)
            .Include(r => r.Invitations)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (entity == null)
            return NotFound<OpenSessionRequestDetailDto>("الطلب غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, entity, cancellationToken))
            return Unauthorized<OpenSessionRequestDetailDto>("Forbidden");

        if (entity.Status != OpenSessionRequestStatus.Draft)
            return BadRequest<OpenSessionRequestDetailDto>("يمكن نشر المسودات فقط.");

        if (entity.Sessions.Count == 0)
            return BadRequest<OpenSessionRequestDetailDto>("أضف جلسة واحدةً واحدة قبل النشر.");

        if (entity.TotalSessionsCount != entity.Sessions.Count)
            return BadRequest<OpenSessionRequestDetailDto>("عدد الجلسات غير متطابق.");

        if (entity.TargetedTeacherId.HasValue)
        {
            var sessionDtos = entity.Sessions.Select(s => new CreateOpenSessionRequestSessionDto
            {
                SequenceNumber = s.SequenceNumber,
                PreferredDate = s.PreferredDate ?? default,
                TimeSlotId = s.TimeSlotId ?? 0,
                DurationMinutes = s.DurationMinutes,
                QuranContentTypeId = s.QuranContentTypeId,
                QuranLevelId = s.QuranLevelId,
                Notes = s.Notes,
                Units = s.Units.Select(u => new CreateOpenSessionRequestUnitDto
                {
                    ContentUnitId = u.ContentUnitId,
                    LessonId = u.LessonId,
                    IncludesAllLessons = u.IncludesAllLessons,
                }).ToList(),
            }).ToList();

            var err = await _targetedValidator.ValidateAsync(
                entity.TargetedTeacherId.Value, entity.SubjectId, sessionDtos, cancellationToken);
            if (err is not null)
                return BadRequest<OpenSessionRequestDetailDto>(err);
        }

        var now = DateTime.UtcNow;
        // Any invitation rows → PendingInvitations until resolved; else Active
        var status = entity.Invitations.Count > 0
            ? OpenSessionRequestStatus.PendingInvitations
            : OpenSessionRequestStatus.Active;

        entity.Status = status;
        entity.PublishedAt = now;
        entity.ExpiresAt ??= now.AddDays(7);

        await _db.SaveChangesAsync(cancellationToken);

        if (status == OpenSessionRequestStatus.Active)
        {
            if (entity.TargetedTeacherId.HasValue)
                await _targetingService.NotifyTargetedTeacherAsync(
                    entity.Id, entity.TargetedTeacherId.Value, cancellationToken);
            else
                await _targetingService.RunMatchingAndNotifyAsync(entity.Id, cancellationToken);
        }

        var detail = await _db.OpenSessionRequests
            .AsNoTracking()
            .Include(r => r.Student).ThenInclude(s => s!.User)
            .Include(r => r.CreatedByGuardian).ThenInclude(g => g!.User)
            .Include(r => r.Domain)
            .Include(r => r.Subject)
            .Include(r => r.TeachingMode)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.Lesson)
            .Include(r => r.Sessions).ThenInclude(s => s.Units).ThenInclude(u => u.ContentUnit)
            .Include(r => r.Invitations).ThenInclude(i => i.InvitedStudent).ThenInclude(s => s!.User)
            .Include(r => r.Attachments)
            .FirstAsync(r => r.Id == entity.Id, cancellationToken);

        return Success(entity: _mapper.Map<OpenSessionRequestDetailDto>(detail));
    }
}
