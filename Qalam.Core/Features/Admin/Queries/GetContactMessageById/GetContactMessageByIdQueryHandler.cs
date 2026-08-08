using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetContactMessageById;

public class GetContactMessageByIdQueryHandler
    : ResponseHandler, IRequestHandler<GetContactMessageByIdQuery, Response<AdminContactMessageDto>>
{
    private readonly IContactMessageRepository _contactMessages;

    public GetContactMessageByIdQueryHandler(
        IContactMessageRepository contactMessages,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _contactMessages = contactMessages;
    }

    public async Task<Response<AdminContactMessageDto>> Handle(
        GetContactMessageByIdQuery request,
        CancellationToken cancellationToken)
    {
        var m = await _contactMessages.GetByIdTrackedAsync(request.Id, cancellationToken);
        if (m == null)
            return NotFound<AdminContactMessageDto>("Contact message not found.");

        return Success(entity: new AdminContactMessageDto
        {
            Id = m.Id,
            Name = m.Name,
            Phone = m.Phone,
            Email = m.Email,
            Reason = m.Reason,
            Message = m.Message,
            Status = m.Status,
            AdminNote = m.AdminNote,
            CreatedAt = m.CreatedAt,
            ClosedAt = m.ClosedAt,
            ClosedByAdminUserId = m.ClosedByAdminUserId
        });
    }
}
