using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Queries.GetTeacherDetails;

/// <summary>
/// Query to get teacher details with all documents for admin review
/// </summary>
public class GetTeacherDetailsQuery : IRequest<Response<TeacherDetailsDto?>>
{
	/// <summary>
	/// Teacher ID to retrieve details for
	/// </summary>
	public int TeacherId { get; set; }
}
