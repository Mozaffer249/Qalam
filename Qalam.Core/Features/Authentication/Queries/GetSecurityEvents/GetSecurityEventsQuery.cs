using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Identity;
using System.Collections.Generic;

namespace Qalam.Core.Features.Authentication.Queries.GetSecurityEvents
{
    public class GetSecurityEventsQuery : IRequest<Response<List<SecurityEvent>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

