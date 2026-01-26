using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Curriculum.Commands.CreateCurriculum;

public class CreateCurriculumCommand : IRequest<Response<Data.Entity.Education.Curriculum>>
{
    public int DomainId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; } = true;
}
