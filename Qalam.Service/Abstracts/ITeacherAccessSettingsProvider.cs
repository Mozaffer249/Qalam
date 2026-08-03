using Qalam.Data.DTOs.Platform;

namespace Qalam.Service.Abstracts;

public interface ITeacherAccessSettingsProvider
{
    Task<TeacherAccessSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<TeacherAccessSettingsDto> SaveSettingsAsync(
        TeacherAccessSettingsDto settings,
        CancellationToken cancellationToken = default);
}
