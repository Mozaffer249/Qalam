using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.ActivateTeacherAccount;

public class ActivateTeacherAccountCommandHandler : ResponseHandler,
    IRequestHandler<ActivateTeacherAccountCommand, Response<string>>
{
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ILogger<ActivateTeacherAccountCommandHandler> _logger;

    public ActivateTeacherAccountCommandHandler(
        ITeacherRegistrationCompletionService completionService,
        ITeacherRepository teacherRepository,
        ILogger<ActivateTeacherAccountCommandHandler> logger,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _completionService = completionService;
        _teacherRepository = teacherRepository;
        _logger = logger;
    }

    public async Task<Response<string>> Handle(
        ActivateTeacherAccountCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await _teacherRepository.GetByIdAsync(request.TeacherId) == null)
                return NotFound<string>("Teacher not found");

            var (success, errorMessage) = await _completionService.ActivateTeacherAccountAsync(
                request.TeacherId,
                request.UserId,
                cancellationToken);

            if (!success)
                return BadRequest<string>(errorMessage ?? "Teacher account cannot be activated.");

            return Success<string>("Teacher account activated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating teacher account {TeacherId}", request.TeacherId);
            return BadRequest<string>("Failed to activate teacher account");
        }
    }
}
