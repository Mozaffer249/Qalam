using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Education.Commands.UpdateGrade;

public class UpdateGradeCommand : IRequest<Response<Grade>>
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int LevelId { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; }
}
