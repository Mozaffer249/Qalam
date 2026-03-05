using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.SearchStudentsForGroup;

public class SearchStudentsForGroupQueryHandler : ResponseHandler,
    IRequestHandler<SearchStudentsForGroupQuery, Response<List<StudentSearchResultDto>>>
{
    private readonly IStudentRepository _studentRepository;

    public SearchStudentsForGroupQueryHandler(
        IStudentRepository studentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Response<List<StudentSearchResultDto>>> Handle(
        SearchStudentsForGroupQuery request,
        CancellationToken cancellationToken)
    {
        var searchTerm = request.SearchTerm.Trim().ToLower();

        var students = await _studentRepository.GetTableNoTracking()
            .Include(s => s.User)
            .Where(s => s.IsActive && s.User != null &&
                ((s.User.FirstName + " " + s.User.LastName).ToLower().Contains(searchTerm) ||
                 s.User.Email!.ToLower().Contains(searchTerm)))
            .Take(request.MaxResults)
            .Select(s => new StudentSearchResultDto
            {
                StudentId = s.Id,
                FullName = s.User.FirstName + " " + s.User.LastName
            })
            .ToListAsync(cancellationToken);

        return Success(entity: students);
    }
}
