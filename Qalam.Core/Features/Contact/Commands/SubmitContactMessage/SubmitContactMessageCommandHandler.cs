using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Contact.Commands.SubmitContactMessage;

public class SubmitContactMessageCommandHandler : ResponseHandler,
    IRequestHandler<SubmitContactMessageCommand, Response<string>>
{
    private readonly IContactMessageRepository _contactMessages;

    public SubmitContactMessageCommandHandler(
        IContactMessageRepository contactMessages,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _contactMessages = contactMessages;
    }

    public async Task<Response<string>> Handle(
        SubmitContactMessageCommand request,
        CancellationToken cancellationToken)
    {
        var entity = new ContactMessage
        {
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Reason = request.Reason.Trim(),
            Message = request.Message.Trim(),
            Status = ContactMessageStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        await _contactMessages.AddAsync(entity);
        return Success<string>("Your message was sent successfully.");
    }
}
