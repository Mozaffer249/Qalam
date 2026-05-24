using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetMyOpenSessionRequests;

public class GetMyOpenSessionRequestsQueryHandler
    : ResponseHandler, IRequestHandler<GetMyOpenSessionRequestsQuery, Response<List<OpenSessionRequestListItemDto>>>
{
    private readonly ApplicationDBContext _db;
    private readonly IMapper _mapper;

    public GetMyOpenSessionRequestsQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IMapper mapper) : base(sharedLocalizer)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<Response<List<OpenSessionRequestListItemDto>>> Handle(
        GetMyOpenSessionRequestsQuery request,
        CancellationToken cancellationToken)
    {
        // Scope: everything created by this user OR by a guardian whose User is this user.
        // The simplest correct query is: created by current user (RequestedByUserId == userId).
        // That already covers (a) student acting alone and (b) guardian acting for their kid,
        // because the guardian's UserId IS the RequestedByUserId in that case.
        // We DON'T include requests where the user is only an "invited co-student" — those
        // appear in /Student/Invitations (separate endpoint, future).
        var query = _db.OpenSessionRequests
            .AsNoTracking()
            .Where(r => r.RequestedByUserId == request.UserId);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.Student).ThenInclude(s => s!.User)
            .Include(r => r.Subject)
            .Include(r => r.Offers)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Success(entity: _mapper.Map<List<OpenSessionRequestListItemDto>>(items));
    }
}
