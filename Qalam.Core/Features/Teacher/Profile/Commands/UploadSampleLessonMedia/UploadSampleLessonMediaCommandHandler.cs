using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Profile.Commands.UploadSampleLessonMedia;

public class UploadSampleLessonMediaCommandHandler : ResponseHandler,
    IRequestHandler<UploadSampleLessonMediaCommand, Response<SampleLessonMediaUploadResultDto>>
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] VideoExtensions = [".mp4", ".webm"];
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MaxVideoBytes = 25 * 1024 * 1024;

    private readonly ITeacherRepository _teacherRepository;
    private readonly IFileStorageService _fileStorageService;

    public UploadSampleLessonMediaCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        ITeacherRepository teacherRepository,
        IFileStorageService fileStorageService) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Response<SampleLessonMediaUploadResultDto>> Handle(
        UploadSampleLessonMediaCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == 0)
            return Unauthorized<SampleLessonMediaUploadResultDto>("User not authenticated");

        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<SampleLessonMediaUploadResultDto>("Teacher not found");

        if (request.File == null || request.File.Length == 0)
            return BadRequest<SampleLessonMediaUploadResultDto>("No file provided");

        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        byte kind;
        long maxBytes;
        if (ImageExtensions.Contains(extension))
        {
            kind = 1;
            maxBytes = MaxImageBytes;
        }
        else if (VideoExtensions.Contains(extension))
        {
            kind = 2;
            maxBytes = MaxVideoBytes;
        }
        else
        {
            return BadRequest<SampleLessonMediaUploadResultDto>(
                "Invalid file type. Allowed: jpg, jpeg, png, webp, mp4, webm.");
        }

        var allowed = kind == 1 ? ImageExtensions : VideoExtensions;
        var isValid = await _fileStorageService.ValidateFileAsync(
            request.File, allowed, maxBytes);
        if (!isValid)
        {
            return BadRequest<SampleLessonMediaUploadResultDto>(
                kind == 1
                    ? "Invalid image or file too large (max 5MB)."
                    : "Invalid video or file too large (max 25MB).");
        }

        var previousPath = teacher.SampleLessonMediaPath;
        string mediaUrl;
        try
        {
            mediaUrl = await _fileStorageService.SaveSampleLessonMediaAsync(
                request.File, teacher.Id);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<SampleLessonMediaUploadResultDto>(ex.Message);
        }

        teacher.SampleLessonMediaPath = mediaUrl;
        teacher.SampleLessonMediaKind = kind;
        await _teacherRepository.UpdateAsync(teacher);

        if (!string.IsNullOrWhiteSpace(previousPath)
            && !string.Equals(previousPath, mediaUrl, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(previousPath);
            }
            catch
            {
                // Best-effort cleanup of previous object.
            }
        }

        return Success(entity: new SampleLessonMediaUploadResultDto
        {
            MediaUrl = mediaUrl,
            MediaKind = kind == 1 ? "image" : "video",
        });
    }
}
