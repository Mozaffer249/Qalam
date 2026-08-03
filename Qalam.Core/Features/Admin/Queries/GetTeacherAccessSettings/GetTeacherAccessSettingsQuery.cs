using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Platform;

namespace Qalam.Core.Features.Admin.Queries.GetTeacherAccessSettings;

public class GetTeacherAccessSettingsQuery : IRequest<Response<TeacherAccessSettingsDto>>
{
}
