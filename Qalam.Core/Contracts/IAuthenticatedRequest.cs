namespace Qalam.Core.Contracts
{
    /// <summary>
    /// Marker interface for requests that require authentication.
    /// UserId will be automatically populated from JWT token by UserIdentityBehavior.
    /// </summary>
    public interface IAuthenticatedRequest
    {
        int UserId { get; set; }
    }
}
