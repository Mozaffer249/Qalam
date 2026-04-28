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

    public int StudentId { get; set; }
}

