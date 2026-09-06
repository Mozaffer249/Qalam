using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using System.Security.Claims;

namespace Qalam.Core.Features.Contact.Commands.SubmitContactMessage;

public class SubmitContactMessageCommandHandler : ResponseHandler,
    IRequestHandler<SubmitContactMessageCommand, Response<string>>
{
    private readonly IContactMessageRepository _contactMessages;
    private readonly UserManager<User> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SubmitContactMessageCommandHandler(
        IContactMessageRepository contactMessages,
        UserManager<User> userManager,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _contactMessages = contactMessages;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<string>> Handle(
        SubmitContactMessageCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var phone = request.Phone?.Trim() ?? string.Empty;
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        var user = await TryGetAuthenticatedUserAsync();
        if (user != null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.Join(
                    ' ',
                    new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            }

            if (string.IsNullOrWhiteSpace(phone))
                phone = user.PhoneNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(user.Email))
                email = user.Email.Trim();
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            return BadRequest<string>("Name and phone are required");

        var entity = new ContactMessage
        {
            Name = name,
            Phone = phone,
            Email = email,
            Reason = request.Reason.Trim(),
            Message = request.Message.Trim(),
            Status = ContactMessageStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        await _contactMessages.AddAsync(entity);
        return Success<string>("Your message was sent successfully.");
    }

    private async Task<User?> TryGetAuthenticatedUserAsync()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = principal.FindFirst("uid") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId) || userId <= 0)
            return null;

        return await _userManager.FindByIdAsync(userId.ToString());
    }
}
