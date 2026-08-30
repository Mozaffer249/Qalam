using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetPartialDomainActivationCandidates;

public class GetPartialDomainActivationCandidatesQueryHandler : ResponseHandler,
    IRequestHandler<GetPartialDomainActivationCandidatesQuery, Response<List<PartialDomainActivationCandidateDto>>>
{
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ILogger<GetPartialDomainActivationCandidatesQueryHandler> _logger;

    public GetPartialDomainActivationCandidatesQueryHandler(
        ITeacherRegistrationCompletionService completionService,
        ILogger<GetPartialDomainActivationCandidatesQueryHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _completionService = completionService;
        _logger = logger;
    }

    public async Task<Response<List<PartialDomainActivationCandidateDto>>> Handle(
        GetPartialDomainActivationCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var candidates = await _completionService.GetPartialDomainActivationCandidatesAsync(cancellationToken);
            _logger.LogInformation(
                "Found {Count} partial-domain activation candidates",
                candidates.Count);
            return Success(entity: candidates.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching partial-domain activation candidates");
            return BadRequest<List<PartialDomainActivationCandidateDto>>(
                "Error retrieving partial-domain activation candidates");
        }
    }
}
