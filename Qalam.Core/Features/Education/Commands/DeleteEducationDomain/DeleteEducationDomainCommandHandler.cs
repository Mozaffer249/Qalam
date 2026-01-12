using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.DeleteEducationDomain;

public class DeleteEducationDomainCommandHandler : ResponseHandler,
    IRequestHandler<DeleteEducationDomainCommand, Response<bool>>
{
    private readonly IEducationDomainService _domainService;

    public DeleteEducationDomainCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<bool>> Handle(
        DeleteEducationDomainCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _domainService.DeleteDomainAsync(request.Id);
            if (!result)
                return NotFound<bool>("Education domain not found");

            return Deleted<bool>();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
