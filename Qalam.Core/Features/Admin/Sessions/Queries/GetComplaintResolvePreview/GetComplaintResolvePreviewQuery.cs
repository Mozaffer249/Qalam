using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Admin.Sessions.Queries.GetComplaintResolvePreview;

public class GetComplaintResolvePreviewQuery : IRequest<Response<ComplaintResolvePreviewDto>>
{
    public int ScheduleId { get; set; }
    public int ComplaintId { get; set; }
    public SessionComplaintResolution ResolutionCode { get; set; }
    public decimal? RefundAmount { get; set; }
    public int? PaymentId { get; set; }
}
