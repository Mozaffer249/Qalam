using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Platform;

namespace Qalam.Core.Features.Admin.Commands.UpdateTeacherAccessSettings;

public class UpdateTeacherAccessSettingsCommand : IRequest<Response<TeacherAccessSettingsDto>>
{
    public TeacherAccessSettingsDto Settings { get; set; } = new();
}
