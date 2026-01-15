using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Curriculum.Commands.UpdateCurriculum;

public class UpdateCurriculumCommand : IRequest<Response<Data.Entity.Education.Curriculum>>
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; }
}
