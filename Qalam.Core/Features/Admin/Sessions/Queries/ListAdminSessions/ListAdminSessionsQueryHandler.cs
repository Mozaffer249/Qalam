using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Queries.ListAdminSessions;

public class ListAdminSessionsQueryHandler : ResponseHandler,
    IRequestHandler<ListAdminSessionsQuery, Response<List<AdminSessionListItemDto>>>
{
    private readonly IAdminSessionReadRepository _sessions;

    public ListAdminSessionsQueryHandler(
        IAdminSessionReadRepository sessions,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _sessions = sessions;
    }

    public async Task<Response<List<AdminSessionListItemDto>>> Handle(
        ListAdminSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new AdminSessionListFilter
        {
            Status = request.Status,
            TeacherId = request.TeacherId,
            StudentId = request.StudentId,
            EnrollmentId = request.EnrollmentId,
            HasComplaint = request.HasComplaint,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
        };

        var items = await _sessions.ListAsync(filter, cancellationToken);
        return Success(entity: items);
    }
}
