using System;
using System.Collections.Generic;

namespace Qalam.Data.Results
{
    public class JwtAuthResult
    {
        public string AccessToken { get; set; } = default!;
        public RefreshToken RefreshToken { get; set; } = default!;
        public bool RequiresTwoFactor { get; set; } = false;
        public bool IsNewDevice { get; set; } = false;
        public string? DeviceId { get; set; }

        // User information
        public int UserId { get; set; }
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? FullName { get; set; }
        public List<string> Roles { get; set; } = new();
    }
    public class RefreshToken
    {
        public string UserName { get; set; } = default!;
        public string TokenString { get; set; } = default!;
        public DateTime ExpireAt { get; set; }
    }
}

