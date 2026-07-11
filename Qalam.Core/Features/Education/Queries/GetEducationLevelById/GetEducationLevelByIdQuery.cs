using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Queries.GetEducationLevelById;

public class GetEducationLevelByIdQuery : IRequest<Response<EducationLevel>>
{
    public int Id { get; set; }
}
