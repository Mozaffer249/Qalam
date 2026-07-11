using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.Entity.Education;

namespace Qalam.Core.Features.Content.Queries.GetLessonById;

public class GetLessonByIdQuery : IRequest<Response<Lesson>>
{
    public int Id { get; set; }
}
