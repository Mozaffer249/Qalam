using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Commands.UpdateAcademicTerm;

public class UpdateAcademicTermCommand : IRequest<Response<AcademicTerm>>
{
    public int Id { get; set; }
    public int CurriculumId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
}
