using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Payments.Commands.StartAdminPaymentReconciliation;

public class StartAdminPaymentReconciliationCommand : IRequest<Response<AdminPaymentReconciliationRunDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }
    public StartPaymentReconciliationRequestDto Data { get; set; } = new();
}

public class StartAdminPaymentReconciliationCommandHandler : ResponseHandler,
    IRequestHandler<StartAdminPaymentReconciliationCommand, Response<AdminPaymentReconciliationRunDto>>
{
    private readonly IAdminPaymentAuditService _audit;

    public StartAdminPaymentReconciliationCommandHandler(
        IAdminPaymentAuditService audit,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _audit = audit;
    }

    public async Task<Response<AdminPaymentReconciliationRunDto>> Handle(
        StartAdminPaymentReconciliationCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _audit.StartReconciliationAsync(request.Data, request.UserId, cancellationToken);
        var dto = await _audit.GetReconciliationRunAsync(run.Id, cancellationToken)
            ?? new AdminPaymentReconciliationRunDto
            {
                Id = run.Id,
                PaymentProvider = run.PaymentProvider,
                Source = run.Source.ToString(),
                Status = run.Status.ToString(),
                StartedAt = run.StartedAt,
                FinishedAt = run.FinishedAt,
                MatchedCount = run.MatchedCount,
                RepairedCount = run.RepairedCount,
                MismatchCount = run.MismatchCount,
                UnresolvedRemoteCount = run.UnresolvedRemoteCount,
                MissingRemoteCount = run.MissingRemoteCount,
                ErrorSummary = run.ErrorSummary,
                LookbackFromUtc = run.LookbackFromUtc,
                LookbackToUtc = run.LookbackToUtc,
                RemotePaymentsSeen = run.RemotePaymentsSeen,
                RemoteInvoicesSeen = run.RemoteInvoicesSeen,
                ScheduleKey = run.ScheduleKey
            };
        return Success(entity: dto);
    }
}
