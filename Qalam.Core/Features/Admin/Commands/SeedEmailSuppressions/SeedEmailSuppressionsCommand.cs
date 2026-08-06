using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Admin.Commands.SeedEmailSuppressions;

public class SeedEmailSuppressionsCommand : IRequest<Response<SeedEmailSuppressionsResultDto>>
{
    public List<string>? Emails { get; set; }

    /// <summary>
    /// When true (default), also suppress synthetic phone.qalam.local addresses already in Users.
    /// </summary>
    public bool IncludeSyntheticLocal { get; set; } = true;
}

public class SeedEmailSuppressionsResultDto
{
    public int Added { get; set; }
    public int Requested { get; set; }
}
