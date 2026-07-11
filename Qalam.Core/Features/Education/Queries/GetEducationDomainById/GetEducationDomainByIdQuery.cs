using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainById;

public class GetEducationDomainByIdQuery : IRequest<Response<EducationDomainDto>>
{
    public int Id { get; set; }
}
