using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Results;

namespace Qalam.Core.Features.Admin.Queries.GetPendingTeachers;

/// <summary>
/// Query to get paginated list of teachers pending verification or with rejected documents
/// </summary>
public class GetPendingTeachersQuery : IRequest<Response<PaginatedResult<PendingTeacherDto>>>
{
	/// <summary>
	/// Page number (1-based)
	/// </summary>
	public int PageNumber { get; set; } = 1;

	/// <summary>
	/// Number of items per page
	/// </summary>
	public int PageSize { get; set; } = 10;
}
