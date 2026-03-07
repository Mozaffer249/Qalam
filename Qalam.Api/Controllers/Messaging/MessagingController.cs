using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qalam.Api.Base;
using Qalam.Core.Features.Messaging.Commands.SendBulkEmail;
using Qalam.Core.Features.Messaging.Commands.SendBulkPush;
using Qalam.Core.Features.Messaging.Commands.SendBulkSms;
using Qalam.Core.Features.Messaging.Commands.SendEmail;
using Qalam.Core.Features.Messaging.Commands.SendPushNotification;
using Qalam.Core.Features.Messaging.Commands.SendSms;
using Qalam.Core.Features.Messaging.Queries.GetMessageHistory;
using Qalam.Core.Features.Messaging.Queries.GetMessageStatus;
using Qalam.Data.AppMetaData;

namespace Qalam.Api.Controllers.Messaging;

[Authorize(Roles = "Admin,SuperAdmin")]
public class MessagingController : AppControllerBase
{
    #region Email

    [HttpPost(Router.MessagingEmail)]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    [HttpPost(Router.MessagingEmailBulk)]
    public async Task<IActionResult> SendBulkEmail([FromBody] SendBulkEmailCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    #endregion

    #region SMS

    [HttpPost(Router.MessagingSms)]
    public async Task<IActionResult> SendSms([FromBody] SendSmsCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    [HttpPost(Router.MessagingSmsBulk)]
    public async Task<IActionResult> SendBulkSms([FromBody] SendBulkSmsCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    #endregion

    #region Push Notifications

    [HttpPost(Router.MessagingPush)]
    public async Task<IActionResult> SendPush([FromBody] SendPushNotificationCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    [HttpPost(Router.MessagingPushBulk)]
    public async Task<IActionResult> SendBulkPush([FromBody] SendBulkPushCommand command)
    {
        return NewResult(await Mediator.Send(command));
    }

    #endregion

    #region Tracking

    [HttpGet(Router.MessagingStatus)]
    public async Task<IActionResult> GetMessageStatus([FromRoute] string messageId)
    {
        return NewResult(await Mediator.Send(new GetMessageStatusQuery { MessageId = messageId }));
    }

    [HttpGet(Router.MessagingHistory)]
    public async Task<IActionResult> GetMessageHistory([FromQuery] GetMessageHistoryQuery query)
    {
        return NewResult(await Mediator.Send(query));
    }

    #endregion

    #region Health

    [HttpGet(Router.MessagingHealth)]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Messaging", Timestamp = DateTime.UtcNow });
    }

    #endregion
}
