using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Student.CourseCatalog.Queries.GetPublishedCourseById;

public class GetPublishedCourseByIdQuery : IRequest<Response<CourseCatalogDetailDto>>
{
    public int Id { get; set; }
}
