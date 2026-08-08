using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetContactMessagesList;

public class GetContactMessagesListQueryHandler
    : ResponseHandler, IRequestHandler<GetContactMessagesListQuery, Response<List<AdminContactMessageDto>>>
{
    private readonly IContactMessageRepository _contactMessages;

    public GetContactMessagesListQueryHandler(
        IContactMessageRepository contactMessages,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _contactMessages = contactMessages;
    }

    public async Task<Response<List<AdminContactMessageDto>>> Handle(
        GetContactMessagesListQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _contactMessages.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Search,
            string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            cancellationToken);

        var items = page.Items.Select(m => new AdminContactMessageDto
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
        }).ToList();

        return Success(entity: items, Meta: BuildPaginationMeta(page.PageNumber, page.PageSize, page.TotalCount));
    }
}
