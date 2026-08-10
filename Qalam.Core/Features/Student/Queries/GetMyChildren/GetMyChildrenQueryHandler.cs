using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Queries.GetMyChildren;

public class GetMyChildrenQueryHandler : ResponseHandler,
    IRequestHandler<GetMyChildrenQuery, Response<List<ChildStudentDto>>>
{
    private readonly IGuardianChildrenService _guardianChildrenService;

    public GetMyChildrenQueryHandler(
        IGuardianChildrenService guardianChildrenService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianChildrenService = guardianChildrenService;
    }

    public async Task<Response<List<ChildStudentDto>>> Handle(
        GetMyChildrenQuery request,
        CancellationToken cancellationToken)
    {
        var children = await _guardianChildrenService.GetMyChildrenAsync(
            request.UserId,
            cancellationToken);

        if (children == null)
            return NotFound<List<ChildStudentDto>>("Guardian profile not found.");

        return Success(entity: children);
    }
}
