using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Content.Commands.CreateLesson;

public class CreateLessonCommand : IRequest<Response<Lesson>>
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public int OrderIndex { get; set; }
}
