using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Queries.SearchStudents;

public class SearchStudentsQueryHandler : ResponseHandler,
    IRequestHandler<SearchStudentsQuery, Response<PaginatedResult<StudentByEmailDto>>>
{
    private readonly IStudentRepository _studentRepository;

    public SearchStudentsQueryHandler(
        IStudentRepository studentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Response<PaginatedResult<StudentByEmailDto>>> Handle(
        SearchStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var searchTerm = request.SearchTerm.Trim().ToLower();

        var query = _studentRepository.GetTableNoTracking()
            .Include(s => s.User)
            .Include(s => s.Guardian)
                .ThenInclude(g => g!.User)
            .Where(s => s.IsActive && s.User != null &&
                ((s.User.FirstName + " " + s.User.LastName).ToLower().Contains(searchTerm) ||
                 s.User.Email!.ToLower().Contains(searchTerm)));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.User.FirstName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StudentByEmailDto
            {
                StudentId = s.Id,
                FullName = (s.User.FirstName + " " + s.User.LastName).Trim(),
                Email = s.User.Email!,
                GuardianId = s.GuardianId,
                GuardianName = s.Guardian != null
                    ? (s.Guardian.FullName ?? (s.Guardian.User != null
                        ? (s.Guardian.User.FirstName + " " + s.Guardian.User.LastName).Trim()
                        : null))
                    : null
            })
            .ToListAsync(cancellationToken);

        var result = new PaginatedResult<StudentByEmailDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Success(entity: result);
    }
}
