using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Queries.GetGradeById;

public class GetGradeByIdQuery : IRequest<Response<Grade>>
{
    public int Id { get; set; }
}
