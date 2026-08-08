using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.CloseContactMessage;

public class CloseContactMessageCommandHandler
    : ResponseHandler, IRequestHandler<CloseContactMessageCommand, Response<string>>
{
    private readonly IContactMessageRepository _contactMessages;

    public CloseContactMessageCommandHandler(
        IContactMessageRepository contactMessages,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _contactMessages = contactMessages;
    }

    public async Task<Response<string>> Handle(
        CloseContactMessageCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _contactMessages.GetByIdTrackedAsync(request.Id, cancellationToken);
        if (entity == null)
            return NotFound<string>("Contact message not found.");

        if (entity.Status == ContactMessageStatus.Closed)
            return Success<string>("Contact message is already closed.");

        entity.Status = ContactMessageStatus.Closed;
        entity.ClosedAt = DateTime.UtcNow;
        entity.ClosedByAdminUserId = request.UserId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UserId;

        if (!string.IsNullOrWhiteSpace(request.AdminNote))
            entity.AdminNote = request.AdminNote.Trim();

        await _contactMessages.UpdateAsync(entity);
        return Success<string>("Contact message closed.");
    }
}
