using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.UploadOpenSessionRequestAttachment;

public class UploadOpenSessionRequestAttachmentCommandHandler
    : ResponseHandler, IRequestHandler<UploadOpenSessionRequestAttachmentCommand, Response<OpenSessionRequestAttachmentDto>>
{
    // Limits per the BRD and the upstream plan.
    private const int MaxAttachmentsPerRequest = 10;
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;            // 25 MB
    private static readonly string[] AllowedExtensions =
        { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };

    private static readonly OpenSessionRequestStatus[] EditableStatuses =
    {
        OpenSessionRequestStatus.Draft,
        OpenSessionRequestStatus.PendingInvitations,
        OpenSessionRequestStatus.Active,
        OpenSessionRequestStatus.ReceivingOffers,
    };

    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public UploadOpenSessionRequestAttachmentCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IFileStorageService fileStorage,
        IConfiguration configuration,
        IMapper mapper) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<Response<OpenSessionRequestAttachmentDto>> Handle(
        UploadOpenSessionRequestAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate the file (size, extension)
        if (!await _fileStorage.ValidateFileAsync(request.File, AllowedExtensions, MaxFileSizeBytes))
            return BadRequest<OpenSessionRequestAttachmentDto>(
                "الملف غير صالح — يجب أن يكون PDF / DOC / DOCX / PNG / JPG وحجمه أقل من 25 ميجابايت");

        // 2. Load parent request + authorize
        var entity = await _db.OpenSessionRequests
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == request.OpenSessionRequestId, cancellationToken);

        if (entity is null)
            return NotFound<OpenSessionRequestAttachmentDto>("الطلب غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, entity, cancellationToken))
            return Unauthorized<OpenSessionRequestAttachmentDto>("غير مصرح لك بإضافة مرفقات لهذا الطلب");

        if (!EditableStatuses.Contains(entity.Status))
            return BadRequest<OpenSessionRequestAttachmentDto>(
                $"لا يمكن إضافة مرفقات في الحالة الحالية ({entity.Status})");

        if (entity.Attachments.Count >= MaxAttachmentsPerRequest)
            return BadRequest<OpenSessionRequestAttachmentDto>(
                $"الحد الأقصى لعدد المرفقات لكل طلب هو {MaxAttachmentsPerRequest}");

        // 3. First save — get the auto-generated Id so we can build a deterministic OSS key.
        var attachment = new OpenSessionRequestAttachment
        {
            SessionRequestId = entity.Id,
            FileName = request.File.FileName,
            ContentType = request.File.ContentType ?? "application/octet-stream",
            FileSizeBytes = request.File.Length,
            StorageKey = "pending",  // overwritten in step 4
        };
        await _db.OpenSessionRequestAttachments.AddAsync(attachment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // 4. Compute the final OSS key + PublicUrl now that we have the attachment Id.
        //    The consumer uploads to exactly this key; no cross-DB write needed afterward.
        var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        var storageKey = $"open-session-requests/{entity.Id}/{attachment.Id}{ext}";
        var ossPublicBase = _configuration["OssSettings:PublicBaseUrl"]
                          ?? _configuration["OSS_PUBLIC_BASE_URL"]
                          ?? string.Empty;
        var publicUrl = string.IsNullOrEmpty(ossPublicBase)
            ? null
            : $"{ossPublicBase.TrimEnd('/')}/{storageKey}";

        attachment.StorageKey = storageKey;
        attachment.PublicUrl = publicUrl;
        await _db.SaveChangesAsync(cancellationToken);

        // 5. Queue the upload. On queue failure we drop the row so the DB doesn't carry a dangling URL.
        try
        {
            await _fileStorage.QueueOpenSessionRequestAttachmentUploadAsync(
                request.File, entity.Id, attachment.Id, storageKey);
        }
        catch
        {
            _db.OpenSessionRequestAttachments.Remove(attachment);
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }

        return Success(entity: _mapper.Map<OpenSessionRequestAttachmentDto>(attachment));
    }
}
