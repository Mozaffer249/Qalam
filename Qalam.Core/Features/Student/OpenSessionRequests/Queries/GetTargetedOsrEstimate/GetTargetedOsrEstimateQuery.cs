using MediatR;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Pricing;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Queries.GetTargetedOsrEstimate;

/// <summary>
/// Preview the price a student will see for a targeted (specific-teacher) OSR
/// before pressing submit. Non-destructive — does NOT create a PricingSnapshot;
/// the real snapshot is frozen at publish time by
/// <see cref="Qalam.Service.Abstracts.ITargetedOpenSessionRequestPricingService"/>.
/// </summary>
public class GetTargetedOsrEstimateQuery : IRequest<Response<PricingEstimateDto>>, IAuthenticatedRequest
{
    public int UserId { get; set; }

    /// <summary>Targeted teacher's Teacher.Id.</summary>
    public int TeacherId { get; set; }

    /// <summary>Education domain id (resolved by the client from the selected subject).</summary>
    public int DomainId { get; set; }

    /// <summary>Total duration across all preview sessions, in minutes.</summary>
    public int TotalMinutes { get; set; }

    /// <summary>Optional. Omit for a single-student request → "individual".</summary>
    public bool IsGroup { get; set; }
}
