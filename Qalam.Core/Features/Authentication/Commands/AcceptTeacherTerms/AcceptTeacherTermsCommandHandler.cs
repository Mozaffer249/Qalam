using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Identity;

namespace Qalam.Core.Features.Authentication.Commands.AcceptTeacherTerms;

public class AcceptTeacherTermsCommandHandler : ResponseHandler,
    IRequestHandler<AcceptTeacherTermsCommand, Response<string>>
{
    private readonly UserManager<User> _userManager;

    public AcceptTeacherTermsCommandHandler(
        UserManager<User> userManager,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _userManager = userManager;
    }

    public async Task<Response<string>> Handle(
        AcceptTeacherTermsCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return NotFound<string>("User not found.");

        if (user.TermsAcceptedAt == null)
        {
            user.TermsAcceptedAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest<string>(errors);
            }
        }

        return Success(entity: "Terms accepted.");
    }
}
