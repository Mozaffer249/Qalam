using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Common;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.CreateNationality;

public class CreateNationalityCommand : IRequest<Response<NationalityAdminDto>>
{
    public CreateNationalityDto Data { get; set; } = null!;
}
