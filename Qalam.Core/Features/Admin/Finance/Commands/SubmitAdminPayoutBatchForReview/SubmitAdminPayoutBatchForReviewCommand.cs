using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Commands.SubmitAdminPayoutBatchForReview;

public class SubmitAdminPayoutBatchForReviewCommand : IRequest<Response<AdminPayoutBatchDto>>
{
    public int Id { get; set; }
    public int? ReviewedByUserId { get; set; }
}
