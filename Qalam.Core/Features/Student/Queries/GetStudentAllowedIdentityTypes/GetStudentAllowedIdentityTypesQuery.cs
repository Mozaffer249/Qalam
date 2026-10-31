using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Student.Queries.GetStudentAllowedIdentityTypes;

public class GetStudentAllowedIdentityTypesQuery : IRequest<Response<List<int>>>
{
    public int NationalityId { get; set; }
}
