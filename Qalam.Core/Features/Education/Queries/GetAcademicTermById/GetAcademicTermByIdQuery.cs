using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Queries.GetAcademicTermById;

public class GetAcademicTermByIdQuery : IRequest<Response<AcademicTerm>>
{
    public int Id { get; set; }
}
