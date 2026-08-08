using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.ReopenContactMessage;

public class ReopenContactMessageCommandHandler
    : ResponseHandler, IRequestHandler<ReopenContactMessageCommand, Response<string>>
{
    private readonly IContactMessageRepository _contactMessages;

    public ReopenContactMessageCommandHandler(
        IContactMessageRepository contactMessages,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _contactMessages = contactMessages;
    }

    public async Task<Response<string>> Handle(
        ReopenContactMessageCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _contactMessages.GetByIdTrackedAsync(request.Id, cancellationToken);
        if (entity == null)
            return NotFound<string>("Contact message not found.");

        if (entity.Status == ContactMessageStatus.Open)
            return Success<string>("Contact message is already open.");

        entity.Status = ContactMessageStatus.Open;
        entity.ClosedAt = null;
        entity.ClosedByAdminUserId = null;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UserId;

        await _contactMessages.UpdateAsync(entity);
        return Success<string>("Contact message reopened.");
    }
}
