using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Platform;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Commands.UpdateTeacherAccessSettings;

public class UpdateTeacherAccessSettingsCommandHandler : ResponseHandler,
    IRequestHandler<UpdateTeacherAccessSettingsCommand, Response<TeacherAccessSettingsDto>>
{
    private readonly ITeacherAccessSettingsProvider _provider;

    public UpdateTeacherAccessSettingsCommandHandler(
        ITeacherAccessSettingsProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<TeacherAccessSettingsDto>> Handle(
        UpdateTeacherAccessSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var saved = await _provider.SaveSettingsAsync(request.Settings, cancellationToken);
        return Success("Teacher access settings updated successfully", entity: saved);
    }
}
