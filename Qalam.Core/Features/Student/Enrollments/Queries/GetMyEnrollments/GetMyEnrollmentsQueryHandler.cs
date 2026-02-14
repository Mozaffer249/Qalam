using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Queries.GetMyEnrollments;

public class GetMyEnrollmentsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentsQuery, Response<PaginatedResult<EnrollmentListItemDto>>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseEnrollmentRepository _enrollmentRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentsQueryHandler(
        IStudentRepository studentRepository,
        ICourseEnrollmentRepository enrollmentRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _mapper = mapper;
    }

    public async Task<Response<PaginatedResult<EnrollmentListItemDto>>> Handle(
        GetMyEnrollmentsQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student == null)
            return NotFound<PaginatedResult<EnrollmentListItemDto>>("Student not found.");

        var query = _enrollmentRepository.GetByStudentIdQueryable(student.Id);
        var totalCount = await query.CountAsync(cancellationToken);

        var enrollments = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<EnrollmentListItemDto>>(enrollments);

        var result = new PaginatedResult<EnrollmentListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Success(entity: result);
    }
}
