using Qalam.Data.DTOs.Policy;
using Qalam.Service.Models.Policy;

namespace Qalam.Service.Abstracts;

public interface ICancellationPolicyEngine
{
    PolicyDecision Evaluate(PolicyContext context, CancellationPolicyRules rules);

    PolicyDecision ValidateException(
        AdminExceptionRules rules,
        AdminExceptionAction action,
        decimal amount,
        string? reason,
        bool isSuperAdmin);
}
