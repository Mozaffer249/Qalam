using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetOpenSessionRequestById;

public class GetOpenSessionRequestByIdQueryHandler
    : ResponseHandler, IRequestHandler<GetOpenSessionRequestByIdQuery, Response<OpenSessionRequestDetailDto>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IMapper _mapper;

    public GetOpenSessionRequestByIdQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IMapper mapper) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _mapper = mapper;
    }

    public async Task<Response<OpenSessionRequestDetailDto>> Handle(
        GetOpenSessionRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.OpenSessionRequests
            .AsNoTracking()
            .Include(r => r.Student).ThenInclude(s => s!.User)
            .Include(r => r.CreatedByGuardian).ThenInclude(g => g!.User)
            .Include(r => r.Domain)
            .Include(r => r.Subject)
            .Include(r => r.TeachingMode)
            .Include(r => r.TargetedTeacher).ThenInclude(t => t!.User)
            .Include(r => r.Sessions).ThenInclude(s => s.Units)
            .Include(r => r.Invitations).ThenInclude(i => i.InvitedStudent).ThenInclude(s => s!.User)
            .Include(r => r.Attachments)
            .Include(r => r.Offers)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (entity is null)
            return NotFound<OpenSessionRequestDetailDto>("الطلب غير موجود");

        // The current user must own the request (or be the request creator's guardian),
        // or be one of the invited students (so they can see what they were invited to).
        var canSeeAsOwner = await _accessGuard.CanActOnRequestAsync(request.UserId, entity, cancellationToken);
        var isInvitedParty = entity.Invitations.Any(i =>
            i.InvitedStudent != null && i.InvitedStudent.UserId == request.UserId);

        if (!canSeeAsOwner && !isInvitedParty)
            return Unauthorized<OpenSessionRequestDetailDto>("غير مصرح لك بعرض هذا الطلب");

        return Success(entity: _mapper.Map<OpenSessionRequestDetailDto>(entity));
    }
}
