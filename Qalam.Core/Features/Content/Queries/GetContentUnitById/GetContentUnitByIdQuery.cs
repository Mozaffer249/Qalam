using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Content.Queries.GetContentUnitById;

public class GetContentUnitByIdQuery : IRequest<Response<ContentUnit>>
{
    public int Id { get; set; }
}
