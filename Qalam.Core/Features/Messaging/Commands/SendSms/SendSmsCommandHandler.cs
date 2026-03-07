using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Messaging.Commands.SendSms;

public class SendSmsCommandHandler : ResponseHandler, IRequestHandler<SendSmsCommand, Response<string>>
{
    private readonly ISmsService _smsService;
    private readonly IMessageTrackingService _messageTrackingService;

    public SendSmsCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ISmsService smsService,
        IMessageTrackingService messageTrackingService) : base(localizer)
    {
        _smsService = smsService;
        _messageTrackingService = messageTrackingService;
    }

    public async Task<Response<string>> Handle(SendSmsCommand request, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString();

        await _messageTrackingService.LogMessageAsync(
            messageId, MessageType.SMS, request.PhoneNumber, string.Empty, request.Content);

        await _smsService.SendSmsAsync(request.PhoneNumber, request.Content, request.Strategy);

        return Success<string>("SMS processed successfully", messageId);
    }
}
