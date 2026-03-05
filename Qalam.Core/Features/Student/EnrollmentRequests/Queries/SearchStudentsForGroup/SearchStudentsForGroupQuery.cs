using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.SearchStudentsForGroup;

public class SearchStudentsForGroupQuery : IRequest<Response<List<StudentSearchResultDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public string SearchTerm { get; set; } = null!;
    public int MaxResults { get; set; } = 20;
}
