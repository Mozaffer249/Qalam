using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Notifications;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Notifications;

public class GetMyNotificationsQuery : IRequest<Response<UserNotificationsPageDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public bool UnreadOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class MarkMyNotificationReadCommand : IRequest<Response<bool>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public int Id { get; set; }
}

public class MarkAllMyNotificationsReadCommand : IRequest<Response<int>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
}

public class GetMyUnreadNotificationsCountQuery : IRequest<Response<int>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
}

public class MyNotificationHandlers : ResponseHandler,
    IRequestHandler<GetMyNotificationsQuery, Response<UserNotificationsPageDto>>,
    IRequestHandler<MarkMyNotificationReadCommand, Response<bool>>,
    IRequestHandler<MarkAllMyNotificationsReadCommand, Response<int>>,
    IRequestHandler<GetMyUnreadNotificationsCountQuery, Response<int>>
{
    private readonly IUserNotificationService _notifications;

    public MyNotificationHandlers(IUserNotificationService notifications, IStringLocalizer<SharedResources> localizer)
        : base(localizer)
    {
        _notifications = notifications;
    }

    public async Task<Response<UserNotificationsPageDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
        => Success(entity: await _notifications.ListAsync(
            request.UserId, request.UnreadOnly, request.Page, request.PageSize, cancellationToken));

    public async Task<Response<bool>> Handle(MarkMyNotificationReadCommand request, CancellationToken cancellationToken)
        => await _notifications.MarkReadAsync(request.UserId, request.Id, cancellationToken)
            ? Success(entity: true)
            : NotFound<bool>("Notification not found.");

    public async Task<Response<int>> Handle(MarkAllMyNotificationsReadCommand request, CancellationToken cancellationToken)
        => Success(entity: await _notifications.MarkAllReadAsync(request.UserId, cancellationToken));

    public async Task<Response<int>> Handle(GetMyUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
        => Success(entity: await _notifications.UnreadCountAsync(request.UserId, cancellationToken));
}
