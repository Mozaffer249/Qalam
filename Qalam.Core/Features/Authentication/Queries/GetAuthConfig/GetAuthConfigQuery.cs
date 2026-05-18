using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Auth;

namespace Qalam.Core.Features.Authentication.Queries.GetAuthConfig;

public class GetAuthConfigQuery : IRequest<Response<AuthConfigResponseDto>>
{
}
