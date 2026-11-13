using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Helpers;

namespace Qalam.Service.Implementations.LRS.Building;

/// <summary>
/// Builds the xAPI <c>actor</c>. An Agent carries exactly one inverse-functional identifier.
/// NELC matches learners by email, so when a national identity is set we emit mbox (email) +
/// the national ID as the name; the account fallback is only used when there is no email.
/// </summary>
public static class XApiActorFactory
{
    public static XApiActor Agent(LrsSettings settings, User user, string? identityNumber = null)
    {
        if (!string.IsNullOrEmpty(identityNumber))
        {
            if (!string.IsNullOrEmpty(user.Email))
                return new XApiActor { Name = identityNumber, Mbox = $"mailto:{user.Email}" };

            return new XApiActor
            {
                Name = identityNumber,
                Account = new XApiAccount { HomePage = settings.PlatformIri, Name = identityNumber }
            };
        }

        return new XApiActor
        {
            Name = user.Email ?? $"{user.FirstName} {user.LastName}".Trim(),
            Mbox = !string.IsNullOrEmpty(user.Email) ? $"mailto:{user.Email}" : null,
            Account = string.IsNullOrEmpty(user.Email)
                ? new XApiAccount { HomePage = settings.PlatformIri, Name = $"user-{user.Id}" }
                : null
        };
    }
}
