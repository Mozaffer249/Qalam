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

    /// <summary>When true, only targeted counts; when false, only broadcast. Null = both.</summary>
    public bool? IsTargeted { get; set; }
}
