using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.OpenSessionRequests.Queries.GetAvailableRequestsSummary;

public class GetAvailableRequestsSummaryQuery : IRequest<Response<TeacherInboxSummaryDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
