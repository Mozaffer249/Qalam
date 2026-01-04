using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Authentication.Commands.UpdateProfile
{
    public class UpdateProfileCommand : IRequest<Response<string>>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? Nationality { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}

