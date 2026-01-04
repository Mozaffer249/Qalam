using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Identity;
using System.Collections.Generic;

namespace Qalam.Core.Features.Authentication.Queries.GetTrustedDevices
{
    public class GetTrustedDevicesQuery : IRequest<Response<List<TrustedDevice>>>
    {
    }
}

