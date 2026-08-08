using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.SetContactMessageInProgress;

public class SetContactMessageInProgressCommandHandler
    : ResponseHandler, IRequestHandler<SetContactMessageInProgressCommand, Response<string>>
{
    private readonly IContactMessageRepository _contactMessages;

    public SetContactMessageInProgressCommandHandler(
        IContactMessageRepository contactMessages,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _contactMessages = contactMessages;
    }

    public async Task<Response<string>> Handle(
        SetContactMessageInProgressCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _contactMessages.GetByIdTrackedAsync(request.Id, cancellationToken);
        if (entity == null)
            return NotFound<string>("Contact message not found.");

        if (entity.Status == ContactMessageStatus.InProgress)
            return Success<string>("Contact message is already in progress.");

        entity.Status = ContactMessageStatus.InProgress;
        entity.ClosedAt = null;
        entity.ClosedByAdminUserId = null;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UserId;

        await _contactMessages.UpdateAsync(entity);
        return Success<string>("Contact message marked in progress.");
    }
}
