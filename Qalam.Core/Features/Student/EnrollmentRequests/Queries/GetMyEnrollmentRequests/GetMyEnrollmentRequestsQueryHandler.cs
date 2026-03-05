using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Queries.GetMyEnrollmentRequests;

public class GetMyEnrollmentRequestsQueryHandler : ResponseHandler,
    IRequestHandler<GetMyEnrollmentRequestsQuery, Response<PaginatedResult<EnrollmentRequestListItemDto>>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IMapper _mapper;

    public GetMyEnrollmentRequestsQueryHandler(
        IStudentRepository studentRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IGuardianRepository guardianRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _studentRepository = studentRepository;
        _requestRepository = requestRepository;
        _guardianRepository = guardianRepository;
        _mapper = mapper;
    }

    public async Task<Response<PaginatedResult<EnrollmentRequestListItemDto>>> Handle(
        GetMyEnrollmentRequestsQuery request,
        CancellationToken cancellationToken)
    {
        // Collect all student IDs the current user is authorized to view
        var authorizedStudentIds = new List<int>();

        // Check if user has a student profile (adult student or "Both" account type)
        var student = await _studentRepository.GetByUserIdAsync(request.UserId);
        if (student != null)
            authorizedStudentIds.Add(student.Id);

        // Check if user is a guardian — include their children's IDs
        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            authorizedStudentIds.AddRange(children.Select(c => c.Id));
        }

        if (authorizedStudentIds.Count == 0)
            return NotFound<PaginatedResult<EnrollmentRequestListItemDto>>("No student profile found.");

        authorizedStudentIds = authorizedStudentIds.Distinct().ToList();

        // If a specific StudentId filter is provided, validate authorization
        if (request.StudentId.HasValue && request.StudentId.Value > 0)
        {
            if (!authorizedStudentIds.Contains(request.StudentId.Value))
                return BadRequest<PaginatedResult<EnrollmentRequestListItemDto>>("You are not authorized to view requests for this student.");

            authorizedStudentIds = new List<int> { request.StudentId.Value };
        }

        var query = _requestRepository.GetTableNoTracking()
            .Include(r => r.Course).ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course).ThenInclude(c => c.SessionType)
            .Where(r => authorizedStudentIds.Contains(r.RequestedByStudentId))
            .OrderByDescending(r => r.CreatedAt);

        if (request.Status.HasValue)
            query = (IOrderedQueryable<Data.Entity.Course.CourseEnrollmentRequest>)
                query.Where(r => r.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var requests = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<EnrollmentRequestListItemDto>>(requests);

        var result = new PaginatedResult<EnrollmentRequestListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
        return Success(entity: result);
    }
}
