using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.BulkActivatePartialDomainTeachers;

public class BulkActivatePartialDomainTeachersCommandHandler : ResponseHandler,
    IRequestHandler<BulkActivatePartialDomainTeachersCommand, Response<BulkActivatePartialDomainTeachersResultDto>>
{
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ILogger<BulkActivatePartialDomainTeachersCommandHandler> _logger;

    public BulkActivatePartialDomainTeachersCommandHandler(
        ITeacherRegistrationCompletionService completionService,
        ILogger<BulkActivatePartialDomainTeachersCommandHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _completionService = completionService;
        _logger = logger;
    }

    public async Task<Response<BulkActivatePartialDomainTeachersResultDto>> Handle(
        BulkActivatePartialDomainTeachersCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _completionService.BulkActivatePartialDomainTeachersAsync(
                request.UserId,
                cancellationToken);

            _logger.LogInformation(
                "Bulk partial-domain activation by admin {AdminId}: {Activated} activated, {Failures} failed",
                request.UserId,
                result.ActivatedCount,
                result.Failures.Count);

            return Success(entity: result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk partial-domain teacher activation");
            return BadRequest<BulkActivatePartialDomainTeachersResultDto>(
                "Failed to bulk activate teachers");
        }
    }
}
