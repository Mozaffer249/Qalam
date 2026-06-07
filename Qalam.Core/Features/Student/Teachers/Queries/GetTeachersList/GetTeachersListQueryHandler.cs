using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetTeachersList;

public class GetTeachersListQueryHandler : ResponseHandler,
    IRequestHandler<GetTeachersListQuery, Response<List<TeacherCardDto>>>
{
    private const int MaxPageSize = 50;

    private readonly ITeacherRepository _teacherRepository;

    public GetTeachersListQueryHandler(
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<List<TeacherCardDto>>> Handle(
        GetTeachersListQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => request.PageSize
        };

        var filters = new TeacherSearchFilters(
            SubjectId: request.SubjectId,
            DomainId: request.DomainId,
            LevelId: request.LevelId,
            GradeId: request.GradeId,
            QuranContentTypeId: request.QuranContentTypeId,
            QuranLevelId: request.QuranLevelId,
            Location: request.Location,
            MinRating: request.MinRating,
            Search: request.Search,
            SortBy: request.SortBy,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var result = await _teacherRepository.SearchAsync(filters, cancellationToken);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
