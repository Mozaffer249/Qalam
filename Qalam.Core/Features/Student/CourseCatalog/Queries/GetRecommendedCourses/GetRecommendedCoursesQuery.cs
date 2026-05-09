using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetRecommendedCourses;

public class GetRecommendedCoursesQuery : IRequest<Response<List<CourseCatalogItemDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// Learner to personalize recommendations (domain). Omit or 0 = use the authenticated user's linked student profile.
    /// Guardians without their own student row must pass an explicit child id.
    /// </summary>
    public int StudentId { get; set; }
}

