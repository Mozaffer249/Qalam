using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.DeleteAvailabilityException;

public class DeleteAvailabilityExceptionCommandHandler : ResponseHandler,
    IRequestHandler<DeleteAvailabilityExceptionCommand, Response<string>>
{
    private readonly ITeacherAvailabilityRepository _availabilityRepository;
    private readonly ITeacherRepository _teacherRepository;

    public DeleteAvailabilityExceptionCommandHandler(
        ITeacherAvailabilityRepository availabilityRepository,
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _availabilityRepository = availabilityRepository;
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<string>> Handle(
        DeleteAvailabilityExceptionCommand request,
        CancellationToken cancellationToken)
    {
        // Get teacher from UserId
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
        {
            return NotFound<string>("Teacher not found");
        }

        // Delete exception (ensures it belongs to this teacher)
        var deleted = await _availabilityRepository.RemoveExceptionAsync(request.ExceptionId, teacher.Id);

        if (!deleted)
        {
            return NotFound<string>("Exception not found or does not belong to this teacher");
        }

        return Success<string>("Availability exception deleted successfully");
    }
}
