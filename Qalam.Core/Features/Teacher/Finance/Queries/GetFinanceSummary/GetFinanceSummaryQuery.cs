using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Finance.Queries.GetFinanceSummary;

public class GetFinanceSummaryQuery : IRequest<Response<TeacherFinanceSummaryDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
