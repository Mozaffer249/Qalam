using Microsoft.AspNetCore.Mvc;
using Qalam.Service.Abstracts;

namespace Qalam.Api.Controllers.Test;

/// <summary>
/// Test endpoint for file upload queue — NO AUTH REQUIRED
/// Remove this controller before production deployment
/// </summary>
[ApiController]
[Route("Api/Test/[controller]")]
public class FileUploadTestController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FileUploadTestController> _logger;

    public FileUploadTestController(
        IFileStorageService fileStorageService,
        ILogger<FileUploadTestController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Test file upload via RabbitMQ queue → MessagingApi → Wasabi
    /// Upload any file and it will be queued for Wasabi upload
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> TestUpload(IFormFile file, [FromQuery] int teacherId = 999, [FromQuery] int documentId = 0)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Error = "No file provided" });

        _logger.LogInformation("========== TEST FILE UPLOAD ==========");
        _logger.LogInformation("File: {FileName}, Size: {Size} bytes, ContentType: {ContentType}",
            file.FileName, file.Length, file.ContentType);
        _logger.LogInformation("TeacherId: {TeacherId}, DocumentId: {DocumentId}", teacherId, documentId);

        try
        {
            // Validate
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".txt", ".docx" };
            var isValid = await _fileStorageService.ValidateFileAsync(file, allowedExtensions, 10 * 1024 * 1024);

            if (!isValid)
            {
                _logger.LogWarning("File validation failed for: {FileName}", file.FileName);
                return BadRequest(new { Error = "File validation failed (check type/size)" });
            }

            _logger.LogInformation("File validated OK. Queueing to RabbitMQ...");

            // Queue to RabbitMQ → MessagingApi → Wasabi
            await _fileStorageService.QueueTeacherDocumentUploadAsync(
                file, teacherId, "test-upload", documentId);

            _logger.LogInformation("File queued successfully to file-upload-queue!");
            _logger.LogInformation("========== QUEUE COMPLETE ==========");

            return Ok(new
            {
                Message = "File queued for Wasabi upload",
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                TeacherId = teacherId,
                DocumentId = documentId,
                Queue = "file-upload-queue",
                Status = "Queued — check MessagingApi logs for upload result"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue file upload");
            return StatusCode(500, new
            {
                Error = "Failed to queue file upload",
                Detail = ex.Message
            });
        }
    }
}
