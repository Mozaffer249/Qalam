using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Finance.Queries.GetAdminTeacherFinanceSummary;

public class GetAdminTeacherFinanceSummaryQuery : IRequest<Response<AdminTeacherFinanceSummaryDto>>
{
    public int TeacherId { get; set; }
}
