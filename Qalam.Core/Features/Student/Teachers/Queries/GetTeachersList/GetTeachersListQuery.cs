using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetTeachersList;

/// <summary>
/// Paginated browse of active teachers. Every filter is optional and AND-combined when supplied.
/// Used as the picker source for <c>CreateOpenSessionRequestDto.TargetedTeacherId</c> in Scenario 2.
/// </summary>
public class GetTeachersListQuery : IRequest<Response<List<TeacherCardDto>>>
{
    public int? SubjectId { get; set; }
    public int? DomainId { get; set; }
    public int? LevelId { get; set; }
    public int? GradeId { get; set; }
    public int? QuranContentTypeId { get; set; }
    public int? QuranLevelId { get; set; }
    public TeacherLocation? Location { get; set; }
    public decimal? MinRating { get; set; }
    public string? Search { get; set; }

    public TeacherSearchSort SortBy { get; set; } = TeacherSearchSort.Rating;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
