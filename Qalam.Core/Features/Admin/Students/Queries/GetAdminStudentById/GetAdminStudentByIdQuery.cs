using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Students.Queries.GetAdminStudentById;

public class GetAdminStudentByIdQuery : IRequest<Response<AdminStudentDetailDto?>>
{
    public int StudentId { get; set; }
}
