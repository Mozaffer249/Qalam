using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Queries.GetChildFile;

/// <summary>
/// Composite child file: attendance aggregates, upcoming sessions, documents.
/// </summary>
public class GetChildFileQuery : IRequest<Response<ChildFileDetailDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int StudentId { get; set; }

    /// <summary>Max upcoming sessions to return (default 5, clamped 1–50).</summary>
    public int UpcomingTake { get; set; } = 5;
}
