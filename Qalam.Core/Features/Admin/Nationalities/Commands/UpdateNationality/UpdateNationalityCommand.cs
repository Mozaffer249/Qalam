using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Common;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.UpdateNationality;

public class UpdateNationalityCommand : IRequest<Response<NationalityAdminDto>>
{
    public int Id { get; set; }
    public UpdateNationalityDto Data { get; set; } = null!;
}
