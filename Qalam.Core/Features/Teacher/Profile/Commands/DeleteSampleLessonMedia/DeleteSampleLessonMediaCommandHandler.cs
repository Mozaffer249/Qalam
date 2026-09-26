using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Profile.Commands.DeleteSampleLessonMedia;

public class DeleteSampleLessonMediaCommandHandler : ResponseHandler,
    IRequestHandler<DeleteSampleLessonMediaCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IFileStorageService _fileStorageService;

    public DeleteSampleLessonMediaCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        IFileStorageService fileStorageService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Response<string>> Handle(
        DeleteSampleLessonMediaCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == 0)
            return Unauthorized<string>("User not authenticated");

        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher not found");

        var previousPath = teacher.SampleLessonMediaPath;
        teacher.SampleLessonMediaPath = null;
        teacher.SampleLessonMediaKind = null;
        await _teacherRepository.UpdateAsync(teacher);

        if (!string.IsNullOrWhiteSpace(previousPath))
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(previousPath);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        return Success<string>("Sample lesson media removed.");
    }
}
