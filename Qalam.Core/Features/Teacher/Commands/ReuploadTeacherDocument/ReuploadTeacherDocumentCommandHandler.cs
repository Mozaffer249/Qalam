using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.ReuploadTeacherDocument;

public class ReuploadTeacherDocumentCommandHandler : ResponseHandler,
    IRequestHandler<ReuploadTeacherDocumentCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITeacherManagementService _teacherManagementService;

    public ReuploadTeacherDocumentCommandHandler(
        ITeacherRepository teacherRepository,
        IFileStorageService fileStorageService,
        ITeacherManagementService teacherManagementService,
        IStringLocalizer<SharedResources> sharedLocalizer) : base(sharedLocalizer)
    {
        _teacherRepository = teacherRepository;
        _fileStorageService = fileStorageService;
        _teacherManagementService = teacherManagementService;
    }

    public async Task<Response<string>> Handle(
        ReuploadTeacherDocumentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == 0)
                return Unauthorized<string>("User not authenticated");

            var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
            if (teacher == null)
                return NotFound<string>("Teacher profile not found");

            // Validate file
            if (request.File == null || request.File.Length == 0)
                return BadRequest<string>("No file provided");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var maxSizeBytes = 10 * 1024 * 1024; // 10MB

            var isValid = await _fileStorageService.ValidateFileAsync(
                request.File, allowedExtensions, maxSizeBytes);

            if (!isValid)
                return BadRequest<string>("Invalid file type or file too large. Allowed: jpg, jpeg, png, pdf (max 10MB)");

            // Update document path to pending (Wasabi URL will be set by consumer)
            var result = await _teacherManagementService.ReuploadDocumentAsync(
                teacher.Id, request.DocumentId, "pending-upload");

            if (!result)
                return BadRequest<string>("Failed to re-upload document. Document may not be in rejected status.");

            // Queue file upload to RabbitMQ → MessagingApi → Wasabi
            await _fileStorageService.QueueTeacherDocUploadAsync(
                request.File, teacher.Id, "reupload", request.DocumentId);

            return Success<string>("Document re-uploaded successfully and is pending review");
        }
        catch (Exception ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }
}
