using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Common;

namespace Qalam.Core.Features.Admin.Nationalities.Commands.SetNationalityActive;

public class SetNationalityActiveCommand : IRequest<Response<NationalityAdminDto>>
{
    public int Id { get; set; }
    public SetNationalityActiveDto Data { get; set; } = null!;
}
