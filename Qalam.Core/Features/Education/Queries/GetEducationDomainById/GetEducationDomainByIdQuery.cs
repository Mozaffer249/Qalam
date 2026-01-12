using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainById;

public class GetEducationDomainByIdQuery : IRequest<Response<EducationDomain>>
{
    public int Id { get; set; }
}
