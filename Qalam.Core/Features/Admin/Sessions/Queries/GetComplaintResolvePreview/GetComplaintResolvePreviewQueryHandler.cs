using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Sessions.Queries.GetComplaintResolvePreview;

public class GetComplaintResolvePreviewQueryHandler : ResponseHandler,
    IRequestHandler<GetComplaintResolvePreviewQuery, Response<ComplaintResolvePreviewDto>>
{
    private readonly ISessionComplaintService _complaints;

    public GetComplaintResolvePreviewQueryHandler(
        ISessionComplaintService complaints,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _complaints = complaints;
    }

    public async Task<Response<ComplaintResolvePreviewDto>> Handle(
        GetComplaintResolvePreviewQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _complaints.GetResolvePreviewAsync(
                request.ScheduleId,
                request.ComplaintId,
                request.ResolutionCode,
                request.RefundAmount,
                request.PaymentId,
                cancellationToken);
            return Success(entity: preview);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound<ComplaintResolvePreviewDto>(ex.Message)
                : BadRequest<ComplaintResolvePreviewDto>(ex.Message);
        }
    }
}
