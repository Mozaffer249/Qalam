using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Commands.UpdateEducationDomain;

public class UpdateEducationDomainCommand : IRequest<Response<EducationDomain>>
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; }
}
