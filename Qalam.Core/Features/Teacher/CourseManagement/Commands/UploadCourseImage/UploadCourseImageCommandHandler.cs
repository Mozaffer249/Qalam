using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UploadCourseImage;

public class UploadCourseImageCommandHandler : ResponseHandler,
    IRequestHandler<UploadCourseImageCommand, Response<CourseImageUploadResultDto>>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxSizeBytes = 5 * 1024 * 1024;

    private readonly ITeacherRepository _teacherRepository;
    private readonly IFileStorageService _fileStorageService;

    public UploadCourseImageCommandHandler(
        ITeacherRepository teacherRepository,
        IFileStorageService fileStorageService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Response<CourseImageUploadResultDto>> Handle(
        UploadCourseImageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == 0)
            return Unauthorized<CourseImageUploadResultDto>("User not authenticated");

        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<CourseImageUploadResultDto>("Teacher profile not found");

        if (teacher.Status != TeacherStatus.Active)
            return BadRequest<CourseImageUploadResultDto>("Teacher account is not active.");

        if (request.File == null || request.File.Length == 0)
            return BadRequest<CourseImageUploadResultDto>("No file provided");

        var isValid = await _fileStorageService.ValidateFileAsync(
            request.File, AllowedExtensions, MaxSizeBytes);

        if (!isValid)
            return BadRequest<CourseImageUploadResultDto>(
                "Invalid file type or file too large. Allowed: jpg, jpeg, png, webp (max 5MB)");

        var imageUrl = await _fileStorageService.SaveCourseImageAsync(request.File, teacher.Id);

        return Success(entity: new CourseImageUploadResultDto { ImageUrl = imageUrl });
    }
}
