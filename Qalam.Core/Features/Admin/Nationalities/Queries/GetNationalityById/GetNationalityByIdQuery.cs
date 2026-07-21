using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Common;

namespace Qalam.Core.Features.Admin.Nationalities.Queries.GetNationalityById;

public class GetNationalityByIdQuery : IRequest<Response<NationalityAdminDto>>
{
    public int Id { get; set; }
}
