using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Admin.Queries.GetEmailSuppressions;

public class GetEmailSuppressionsQueryHandler
    : ResponseHandler, IRequestHandler<GetEmailSuppressionsQuery, Response<List<EmailSuppressionListItemDto>>>
{
    private readonly ApplicationDBContext _db;

    public GetEmailSuppressionsQueryHandler(
        ApplicationDBContext db,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _db = db;
    }

    public async Task<Response<List<EmailSuppressionListItemDto>>> Handle(
        GetEmailSuppressionsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize switch
        {
            < 1 => 20,
            > 50 => 50,
            _ => request.PageSize
        };

        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim().ToLowerInvariant();

        var query = _db.EmailSuppressions.AsNoTracking().AsQueryable();

        if (search != null)
        {
            query = query.Where(s =>
                s.Email.Contains(search)
                || (s.Diagnostic != null && s.Diagnostic.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.LastBounceAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new EmailSuppressionListItemDto
            {
                Id = s.Id,
                Email = s.Email,
                Reason = s.Reason,
                Source = s.Source,
                Diagnostic = s.Diagnostic,
                BounceCount = s.BounceCount,
                CreatedAt = s.CreatedAt,
                LastBounceAt = s.LastBounceAt
            })
            .ToListAsync(cancellationToken);

        return Success(entity: items, Meta: BuildPaginationMeta(pageNumber, pageSize, totalCount));
    }
}
