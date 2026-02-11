using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Queries.GetCoursesList;

public class GetCoursesListQueryHandler : ResponseHandler,
    IRequestHandler<GetCoursesListQuery, Response<List<CourseListItemDto>>>
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

    public async Task<Response<List<CourseListItemDto>>> Handle(
        GetCoursesListQuery request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return Unauthorized<List<CourseListItemDto>>("Not authorized.");

        var query = _courseRepository.GetTeacherCoursesQueryable(teacher.Id);

        // Apply filters
        if (request.DomainId.HasValue)
            query = query.Where(c => c.DomainId == request.DomainId.Value);

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        if (request.SubjectId.HasValue)
            query = query.Where(c => c.SubjectId == request.SubjectId.Value);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var courses = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var items = courses.Select(CourseDtoMapper.MapToListItemDto).ToList();

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var paginationMeta = new
        {
            totalCount,
            pageNumber = request.PageNumber,
            pageSize = request.PageSize,
            totalPages,
            hasPreviousPage = request.PageNumber > 1,
            hasNextPage = request.PageNumber < totalPages
        };

        return Success("Courses fetched successfully", entity: items, Meta: paginationMeta);
    }
}
