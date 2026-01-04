using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Queries.GetProfile
{
    public class GetProfileQuery : IRequest<Response<ProfileResponse>>
    {
    }

    public class ProfileResponse
    {
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? Address { get; set; }
        public string? Nationality { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}

