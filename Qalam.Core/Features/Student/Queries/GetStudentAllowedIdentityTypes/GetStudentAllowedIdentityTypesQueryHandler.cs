using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Queries.GetStudentAllowedIdentityTypes;

public class GetStudentAllowedIdentityTypesQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentAllowedIdentityTypesQuery, Response<List<int>>>
{
    private readonly IStudentIdentityService _identityService;

    public GetStudentAllowedIdentityTypesQueryHandler(
        IStudentIdentityService identityService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _identityService = identityService;
    }

    public async Task<Response<List<int>>> Handle(
        GetStudentAllowedIdentityTypesQuery request,
        CancellationToken cancellationToken)
    {
        var types = await _identityService.GetAllowedIdentityTypesAsync(request.NationalityId, cancellationToken);

        if (types.Count == 0)
            return BadRequest<List<int>>("No identity types are configured for this nationality.");

        return Success(entity: types.Select(t => (int)t).ToList());
    }
}
