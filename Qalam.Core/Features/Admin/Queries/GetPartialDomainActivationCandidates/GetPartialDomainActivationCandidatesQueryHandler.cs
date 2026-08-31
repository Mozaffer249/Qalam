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
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize switch
            {
                < 1 => 10,
                > 50 => 50,
                _ => request.PageSize,
            };

            var result = await _completionService.GetPartialDomainActivationCandidatesPagedAsync(
                pageNumber,
                pageSize,
                cancellationToken);
            _logger.LogInformation(
                "Found {Count} partial-domain activation candidates (page {Page} of {TotalPages})",
                result.TotalCount,
                result.PageNumber,
                result.TotalPages);
            return Success(entity: result.Items, Meta: BuildPaginationMeta(
                result.PageNumber,
                result.PageSize,
                result.TotalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching partial-domain activation candidates");
            return BadRequest<List<PartialDomainActivationCandidateDto>>(
                "Error retrieving partial-domain activation candidates");
        }
    }
}
