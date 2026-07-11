using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherReviews;

public class GetStudentTeacherReviewsQuery : IRequest<Response<List<StudentTeacherReviewDto>>>
{
    public int TeacherId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
