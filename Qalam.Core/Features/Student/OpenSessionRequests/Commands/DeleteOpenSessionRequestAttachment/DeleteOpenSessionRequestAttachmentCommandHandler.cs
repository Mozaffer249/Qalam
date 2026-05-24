using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.DeleteOpenSessionRequestAttachment;

public class DeleteOpenSessionRequestAttachmentCommandHandler
    : ResponseHandler, IRequestHandler<DeleteOpenSessionRequestAttachmentCommand, Response<string>>
{
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

    public DeleteOpenSessionRequestAttachmentCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IFileStorageService fileStorage) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _fileStorage = fileStorage;
    }

    public async Task<Response<string>> Handle(
        DeleteOpenSessionRequestAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        var attachment = await _db.OpenSessionRequestAttachments
            .Include(a => a.OpenSessionRequest)
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId
                                   && a.SessionRequestId == request.OpenSessionRequestId,
                                 cancellationToken);

        if (attachment is null)
            return NotFound<string>("المرفق غير موجود");

        if (!await _accessGuard.CanActOnRequestAsync(request.UserId, attachment.OpenSessionRequest, cancellationToken))
            return Unauthorized<string>("غير مصرح لك بحذف هذا المرفق");

        if (!EditableStatuses.Contains(attachment.OpenSessionRequest.Status))
            return BadRequest<string>($"لا يمكن حذف المرفقات في الحالة الحالية ({attachment.OpenSessionRequest.Status})");

        // Best-effort OSS cleanup. If StorageKey is still the placeholder (consumer hasn't run yet)
        // or the file is already gone, we still drop the DB row.
        if (!string.IsNullOrWhiteSpace(attachment.StorageKey))
        {
            try { await _fileStorage.DeleteFileAsync(attachment.StorageKey); }
            catch { /* swallow — the row is what matters here */ }
        }

        _db.OpenSessionRequestAttachments.Remove(attachment);
        await _db.SaveChangesAsync(cancellationToken);

        return Success(entity: "تم حذف المرفق");
    }
}
