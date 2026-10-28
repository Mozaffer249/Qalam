using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.LRS;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Lrs.Commands;

public class RetryLrsStatementCommandHandler : ResponseHandler,
    IRequestHandler<RetryLrsStatementCommand, Response<string>>
{
    private readonly ILrsStatementLogRepository _repo;
    private readonly IRabbitMQService _rabbitMQ;

    public RetryLrsStatementCommandHandler(
        ILrsStatementLogRepository repo,
        IRabbitMQService rabbitMQ,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repo = repo;
        _rabbitMQ = rabbitMQ;
    }

    public async Task<Response<string>> Handle(
        RetryLrsStatementCommand request, CancellationToken cancellationToken)
    {
        var log = await _repo.GetEntityByIdAsync(request.Id, cancellationToken);
        if (log == null)
            return NotFound<string>("LRS statement not found.");

        if (log.Status is not (LrsStatementStatus.Failed or LrsStatementStatus.DeadLettered))
            return BadRequest<string>("Only failed or dead-lettered statements can be retried.");

        if (string.IsNullOrEmpty(log.StatementJson))
            return BadRequest<string>("Statement JSON is not available for retry.");

        log.Status = LrsStatementStatus.Queued;
        log.RetryCount = 0;
        log.ErrorMessage = null;
        log.HttpStatusCode = null;
        await _repo.SaveChangesAsync(cancellationToken);

        await _rabbitMQ.QueueLrsStatementAsync(new LrsStatementMessage
        {
            StatementJson = log.StatementJson,
            Verb = log.Verb,
            ActorName = log.ActorIdentifier,
            ObjectId = log.ObjectIri,
            Timestamp = DateTime.UtcNow,
            SourceEntityType = log.SourceEntityType,
            SourceEntityId = log.SourceEntityId,
            QueuedAt = DateTime.UtcNow
        });

        return Success(entity: "Statement re-queued for retry.");
    }
}
