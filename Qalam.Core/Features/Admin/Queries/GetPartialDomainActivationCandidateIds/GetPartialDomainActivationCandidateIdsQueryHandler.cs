using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetPartialDomainActivationCandidateIds;

public class GetPartialDomainActivationCandidateIdsQueryHandler : ResponseHandler,
    IRequestHandler<GetPartialDomainActivationCandidateIdsQuery, Response<List<int>>>
{
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ILogger<GetPartialDomainActivationCandidateIdsQueryHandler> _logger;

    public GetPartialDomainActivationCandidateIdsQueryHandler(
        ITeacherRegistrationCompletionService completionService,
        ILogger<GetPartialDomainActivationCandidateIdsQueryHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _completionService = completionService;
        _logger = logger;
    }

    public async Task<Response<List<int>>> Handle(
        GetPartialDomainActivationCandidateIdsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ids = await _completionService.GetPartialDomainActivationCandidateIdsAsync(cancellationToken);
            _logger.LogInformation(
                "Found {Count} partial-domain activation candidate IDs",
                ids.Count);
            return Success(entity: ids.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching partial-domain activation candidate IDs");
            return BadRequest<List<int>>(
                "Error retrieving partial-domain activation candidate IDs");
        }
    }
}
