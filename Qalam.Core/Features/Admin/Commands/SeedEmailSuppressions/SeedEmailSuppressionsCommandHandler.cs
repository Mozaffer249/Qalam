using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.SeedEmailSuppressions;

public class SeedEmailSuppressionsCommandHandler
    : ResponseHandler, IRequestHandler<SeedEmailSuppressionsCommand, Response<SeedEmailSuppressionsResultDto>>
{
    private readonly IEmailSuppressionService _suppressionService;
    private readonly UserManager<User> _userManager;

    public SeedEmailSuppressionsCommandHandler(
        IEmailSuppressionService suppressionService,
        UserManager<User> userManager,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _suppressionService = suppressionService;
        _userManager = userManager;
    }

    public async Task<Response<SeedEmailSuppressionsResultDto>> Handle(
        SeedEmailSuppressionsCommand request,
        CancellationToken cancellationToken)
    {
        var emails = new List<string>();
        if (request.Emails != null)
            emails.AddRange(request.Emails);

        if (request.IncludeSyntheticLocal)
        {
            var synthetic = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Email != null && u.Email.EndsWith("@phone.qalam.local"))
                .Select(u => u.Email!)
                .Take(5000)
                .ToListAsync(cancellationToken);
            emails.AddRange(synthetic);
        }

        var distinct = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var added = await _suppressionService.SeedAsync(
            distinct,
            EmailSuppressionReason.Manual,
            EmailSuppressionSource.Admin,
            "Seeded from admin SeedEmailSuppressions",
            cancellationToken);

        if (request.IncludeSyntheticLocal)
        {
            var synthetic = distinct.Where(e =>
                e.EndsWith("@phone.qalam.local", StringComparison.OrdinalIgnoreCase));
            foreach (var addr in synthetic)
            {
                await _suppressionService.SuppressAsync(
                    addr,
                    EmailSuppressionReason.SyntheticLocal,
                    EmailSuppressionSource.System,
                    "Synthetic phone-only account email",
                    cancellationToken);
            }
        }

        return Success(entity: new SeedEmailSuppressionsResultDto
        {
            Added = added,
            Requested = distinct.Count
        });
    }
}
