using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Commands.CreateEducationLevel;

public class CreateEducationLevelCommand : IRequest<Response<EducationLevel>>
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int DomainId { get; set; }
    public int? CurriculumId { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
}
