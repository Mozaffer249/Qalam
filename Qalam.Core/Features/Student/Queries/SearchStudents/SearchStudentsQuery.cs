using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Queries.SearchStudents;

public class SearchStudentsQuery : IRequest<Response<List<StudentByEmailDto>>>
{
    public string SearchTerm { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
