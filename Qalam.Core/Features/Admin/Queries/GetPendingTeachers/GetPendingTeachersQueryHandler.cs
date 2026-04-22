using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Admin;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetPendingTeachers;

public class GetPendingTeachersQueryHandler : ResponseHandler,
    IRequestHandler<GetPendingTeachersQuery, Response<List<PendingTeacherDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ILogger<GetPendingTeachersQueryHandler> _logger;

    public GetPendingTeachersQueryHandler(
        ITeacherRepository teacherRepository,
        ILogger<GetPendingTeachersQueryHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _logger = logger;
    }

    public async Task<Response<List<PendingTeacherDto>>> Handle(
        GetPendingTeachersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Fetching pending teachers - Page: {PageNumber}, PageSize: {PageSize}",
                request.PageNumber,
                request.PageSize);

            var query = _teacherRepository.GetPendingTeachersQueryable();
            var totalCount = await _teacherRepository.CountAsync(query);

            var teachers = await _teacherRepository.GetPendingTeachersDtoAsync(
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation(
                "Successfully fetched {Count} pending teachers out of {Total}",
                teachers.Count,
                totalCount);

            return Success<List<PendingTeacherDto>>(
                entity: teachers,
                Meta: BuildPaginationMeta(request.PageNumber, request.PageSize, totalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending teachers");
            return BadRequest<List<PendingTeacherDto>>("Error retrieving pending teachers");
        }
    }
}
