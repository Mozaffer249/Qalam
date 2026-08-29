using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Sessions.Queries.ListStudentSessions;

public class ListStudentSessionsQueryHandler : ResponseHandler,
    IRequestHandler<ListStudentSessionsQuery, Response<List<StudentSessionListItemDto>>>
{
    private readonly IStudentSessionReadRepository _sessionReadRepository;

    public ListStudentSessionsQueryHandler(
        IStudentSessionReadRepository sessionReadRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _sessionReadRepository = sessionReadRepository;
    }

    public async Task<Response<List<StudentSessionListItemDto>>> Handle(
        ListStudentSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _sessionReadRepository.ListForStudentUserAsync(request.UserId, cancellationToken);
        return Success(entity: items);
    }
}
