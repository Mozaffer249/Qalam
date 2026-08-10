using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Queries.GetChildFile;

public class GetChildFileQueryHandler : ResponseHandler,
    IRequestHandler<GetChildFileQuery, Response<ChildFileDetailDto>>
{
    private readonly IGuardianChildrenService _guardianChildrenService;

    public GetChildFileQueryHandler(
        IGuardianChildrenService guardianChildrenService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianChildrenService = guardianChildrenService;
    }

    public async Task<Response<ChildFileDetailDto>> Handle(
        GetChildFileQuery request,
        CancellationToken cancellationToken)
    {
        var detail = await _guardianChildrenService.GetChildFileAsync(
            request.UserId,
            request.StudentId,
            request.UpcomingTake,
            cancellationToken);

        if (detail == null)
            return NotFound<ChildFileDetailDto>("Child not found.");

        return Success(entity: detail);
    }
}
