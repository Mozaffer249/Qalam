using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Commands.CreateEducationDomain;

public class CreateEducationDomainCommand : IRequest<Response<EducationDomainDto>>
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; } = true;
    public EducationRuleDto? EducationRule { get; set; }
}
