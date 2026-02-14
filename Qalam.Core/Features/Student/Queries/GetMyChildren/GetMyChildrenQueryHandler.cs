using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Queries.GetMyChildren;

public class GetMyChildrenQueryHandler : ResponseHandler,
    IRequestHandler<GetMyChildrenQuery, Response<List<ChildStudentDto>>>
{
    private readonly IGuardianRepository _guardianRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public GetMyChildrenQueryHandler(
        IGuardianRepository guardianRepository,
        IStudentRepository studentRepository,
        IMapper mapper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianRepository = guardianRepository;
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    public async Task<Response<List<ChildStudentDto>>> Handle(
        GetMyChildrenQuery request,
        CancellationToken cancellationToken)
    {
        var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
        if (guardian == null)
            return NotFound<List<ChildStudentDto>>("Guardian profile not found.");

        var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
        var childrenDtos = _mapper.Map<List<ChildStudentDto>>(children);

        return Success(entity: childrenDtos);
    }
}
