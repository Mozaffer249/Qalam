using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Commands.CreateAcademicTerm;

public class CreateAcademicTermCommand : IRequest<Response<AcademicTerm>>
{
    public int CurriculumId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public bool IsMandatory { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
