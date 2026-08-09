using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Helpers;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Identity;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.AcceptTeacherTerms;

public class AcceptTeacherTermsCommandHandler : ResponseHandler,
    IRequestHandler<AcceptTeacherTermsCommand, Response<string>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILegalConsentService _consentService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AcceptTeacherTermsCommandHandler(
        UserManager<User> userManager,
        ILegalConsentService consentService,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _userManager = userManager;
        _consentService = consentService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<string>> Handle(
        AcceptTeacherTermsCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return NotFound<string>("User not found.");

        var ctx = _httpContextAccessor.HttpContext;
        await _consentService.AcceptRequiredAsync(
            request.UserId,
            source: "teacher-accept-terms",
            ipAddress: ClientIpHelper.GetClientIpAddress(ctx),
            userAgent: ClientIpHelper.GetUserAgent(ctx),
            cancellationToken);

        return Success(entity: "Terms accepted.");
    }
}
