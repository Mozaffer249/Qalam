using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Course.Queries.GetCoursesList;

public class GetCoursesListQueryHandler : ResponseHandler,
    IRequestHandler<GetCoursesListQuery, Response<PaginatedResult<CourseListItemDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseRepository _courseRepository;

    public GetCoursesListQueryHandler(
        ITeacherRepository teacherRepository,
        ICourseRepository courseRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _courseRepository = courseRepository;
    }

    public async Task<Response<PaginatedResult<CourseListItemDto>>> Handle(
        GetCoursesListQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<PaginatedResult<CourseListItemDto>>("Teacher not found.");

        var baseQuery = _courseRepository.GetTableNoTracking()
            .Where(c => c.TeacherId == teacher.Id);

        if (request.DomainId.HasValue)
            baseQuery = baseQuery.Where(c => c.DomainId == request.DomainId.Value);
        if (request.Status.HasValue)
            baseQuery = baseQuery.Where(c => c.Status == request.Status.Value);
        if (request.SubjectId.HasValue)
            baseQuery = baseQuery.Where(c => c.SubjectId == request.SubjectId.Value);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Include(c => c.Domain)
            .Include(c => c.Subject)
            .Include(c => c.TeachingMode)
            .Include(c => c.SessionType)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseListItemDto
            {
                Id = c.Id,
                Title = c.Title,
                DescriptionShort = c.Description != null && c.Description.Length > 100 ? c.Description.Substring(0, 100) + "..." : c.Description,
                TeacherId = c.TeacherId,
                DomainId = c.DomainId,
                DomainNameEn = c.Domain != null ? c.Domain.NameEn : null,
                SubjectId = c.SubjectId,
                SubjectNameEn = c.Subject != null ? c.Subject.NameEn : null,
                TeachingModeId = c.TeachingModeId,
                TeachingModeNameEn = c.TeachingMode != null ? c.TeachingMode.NameEn : null,
                SessionTypeId = c.SessionTypeId,
                SessionTypeNameEn = c.SessionType != null ? c.SessionType.NameEn : null,
                Status = c.Status,
                IsActive = c.IsActive,
                Price = c.Price,
                StartDate = c.StartDate,
                EndDate = c.EndDate
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<CourseListItemDto>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Success(entity: result);
    }
}
