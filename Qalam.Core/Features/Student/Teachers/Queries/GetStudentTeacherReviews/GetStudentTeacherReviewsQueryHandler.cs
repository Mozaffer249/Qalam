using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherReviews;

public class GetStudentTeacherReviewsQueryHandler : ResponseHandler,
    IRequestHandler<GetStudentTeacherReviewsQuery, Response<List<StudentTeacherReviewDto>>>
{
    private const int MaxPageSize = 50;
    private readonly ITeacherRepository _teacherRepository;

    public GetStudentTeacherReviewsQueryHandler(
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<List<StudentTeacherReviewDto>>> Handle(
        GetStudentTeacherReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => request.PageSize
        };

        var result = await _teacherRepository.GetStudentReviewsAsync(
            request.TeacherId, pageNumber, pageSize, cancellationToken);

        if (result.TotalCount == 0)
        {
            var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
            if (teacher is null || teacher.Status != Qalam.Data.Entity.Common.Enums.TeacherStatus.Active || !teacher.IsActive)
                return NotFound<List<StudentTeacherReviewDto>>("Teacher not found.");
        }

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
