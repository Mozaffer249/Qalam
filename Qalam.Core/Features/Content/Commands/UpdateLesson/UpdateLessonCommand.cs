using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Content.Commands.UpdateLesson;

public class UpdateLessonCommand : IRequest<Response<Lesson>>
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
    public int? QuranContentTypeId { get; set; }
    public int? QuranLevelId { get; set; }
}
