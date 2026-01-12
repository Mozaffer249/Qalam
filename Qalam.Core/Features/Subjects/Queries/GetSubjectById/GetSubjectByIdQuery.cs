using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Subjects.Queries.GetSubjectById;

public class GetSubjectByIdQuery : IRequest<Response<Subject>>
{
    public int Id { get; set; }
}
