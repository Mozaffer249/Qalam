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

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetMyOpenSessionRequests;

public class GetMyOpenSessionRequestsQueryHandler
    : ResponseHandler, IRequestHandler<GetMyOpenSessionRequestsQuery, Response<List<OpenSessionRequestListItemDto>>>
{
    private readonly ApplicationDBContext _db;
    private readonly IMapper _mapper;
    private readonly IOpenSessionRequestStudentPricingEnricher _pricingEnricher;

    public GetMyOpenSessionRequestsQueryHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IMapper mapper,
        IOpenSessionRequestStudentPricingEnricher pricingEnricher) : base(sharedLocalizer)
    {
        _db = db;
        _mapper = mapper;
        _pricingEnricher = pricingEnricher;
    }

    public async Task<Response<List<OpenSessionRequestListItemDto>>> Handle(
        GetMyOpenSessionRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var query = _db.OpenSessionRequests
            .AsNoTracking()
            .Where(r => r.RequestedByUserId == request.UserId);

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }
        else
        {
            var open = OpenSessionRequestStatusSets.StudentOpen;
            query = request.Scope switch
            {
                OpenSessionRequestScope.Active =>
                    query.Where(r => open.Contains(r.Status)),
                OpenSessionRequestScope.Archived =>
                    query.Where(r => !open.Contains(r.Status)),
                _ => query,
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.Student).ThenInclude(s => s!.User)
            .Include(r => r.Subject)
            .Include(r => r.TeachingMode)
            .Include(r => r.TargetedTeacher).ThenInclude(t => t!.User)
            .Include(r => r.Offers)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<OpenSessionRequestListItemDto>>(items);
        await _pricingEnricher.EnrichListAsync(dtos, items, cancellationToken);

        return Success(
            entity: dtos,
            Meta: BuildPaginationMeta(pageNumber, pageSize, totalCount));
    }
}
