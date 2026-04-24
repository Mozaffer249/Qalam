using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Subjects.Queries.GetSubjectById;

public class GetSubjectByIdQuery : IRequest<Response<SubjectDto>>
{
    public int Id { get; set; }
}
