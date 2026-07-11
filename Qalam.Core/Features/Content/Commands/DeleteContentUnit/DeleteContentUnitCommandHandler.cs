using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Content.Commands.DeleteContentUnit;

public class DeleteContentUnitCommandHandler : ResponseHandler,
    IRequestHandler<DeleteContentUnitCommand, Response<bool>>
{
    private readonly IContentManagementService _contentService;

    public DeleteContentUnitCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IContentManagementService contentService) : base(localizer)
    {
        _contentService = contentService;
    }

    public async Task<Response<bool>> Handle(
        DeleteContentUnitCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _contentService.DeleteContentUnitAsync(request.Id);
            if (!result)
                return NotFound<bool>("Content unit not found");

            return Deleted<bool>();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<bool>(ex.Message);
        }
    }
}
