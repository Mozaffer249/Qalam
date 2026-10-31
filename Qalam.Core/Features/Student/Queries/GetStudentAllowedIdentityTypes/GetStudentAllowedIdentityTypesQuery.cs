using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Student.Queries.GetStudentAllowedIdentityTypes;

public class GetStudentAllowedIdentityTypesQuery : IRequest<Response<List<IdentityType>>>
{
    public int NationalityId { get; set; }
}
