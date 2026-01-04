using MediatR;
using Qalam.Core.Bases;
using System;
using System.Collections.Generic;

namespace Qalam.Core.Features.Authentication.Queries.GetActiveSessions
{
    public class GetActiveSessionsQuery : IRequest<Response<List<SessionResponse>>>
    {
    }

    public class SessionResponse
    {
        public long SessionId { get; set; }
        public string DeviceInfo { get; set; } = default!;
        public string IpAddress { get; set; } = default!;
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsCurrent { get; set; }
    }
}

