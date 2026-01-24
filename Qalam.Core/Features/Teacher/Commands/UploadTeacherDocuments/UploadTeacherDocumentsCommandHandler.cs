using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Validators;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;

public class UploadTeacherDocumentsCommandHandler : ResponseHandler,
    IRequestHandler<UploadTeacherDocumentsCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITeacherRegistrationService _teacherRegistrationService;
    private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

    public UploadTeacherDocumentsCommandHandler(
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        ITeacherRegistrationService teacherRegistrationService,
        IStringLocalizer<SharedResources> sharedLocalizer,
        IStringLocalizer<AuthenticationResources> authLocalizer) : base(sharedLocalizer)
    {
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _teacherRegistrationService = teacherRegistrationService;
        _authLocalizer = authLocalizer;
    }

    public async Task<Response<string>> Handle(
        UploadTeacherDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // UserId is automatically populated by UserIdentityBehavior
            if (request.UserId == 0)
            {
                return Unauthorized<string>("User not authenticated");
            }

            // Get teacher profile for this user
            var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
            if (teacher == null)
            {
                return BadRequest<string>("Teacher profile not found. Please complete Step 3 first.");
            }

            // Validate teacher status - only allow uploads for specific statuses
            if (teacher.Status == TeacherStatus.PendingVerification)
            {
                return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.DocumentsAlreadyPendingVerification]);
            }

            if (teacher.Status == TeacherStatus.Active)
            {
                return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.AccountAlreadyVerified]);
            }

            if (teacher.Status == TeacherStatus.Blocked)
            {
                return Unauthorized<string>(_authLocalizer[AuthenticationResourcesKeys.AccountBlocked]);
            }

            // At this point, status is either AwaitingDocuments or DocumentsRejected (both valid for upload)

            var teacherId = teacher.Id;

            // Validate business rules
            TeacherDocumentBusinessRules.ValidateSaudiIdentityRules(
                request.IsInSaudiArabia,
                request.IdentityType,
                request.IssuingCountryCode,
                _authLocalizer);

            TeacherDocumentBusinessRules.ValidateCertificateCount(request.Certificates.Count, _authLocalizer);

            // Validate identity document uniqueness
            await TeacherDocumentBusinessRules.ValidateIdentityUnique(
                _documentRepository,
                request.IdentityType,
                request.DocumentNumber,
                request.IssuingCountryCode,
                _authLocalizer);

            // Define allowed file extensions and max size (10MB)
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var maxSizeBytes = 10 * 1024 * 1024; // 10MB

            // Validate and save identity document
            var isIdentityValid = await _fileStorageService.ValidateFileAsync(
                request.IdentityDocumentFile,
                allowedExtensions,
                maxSizeBytes);

            if (!isIdentityValid)
            {
                return BadRequest<string>("Identity document file is invalid or too large");
            }

            var identityFilePath = await _fileStorageService.SaveTeacherDocumentAsync(
                request.IdentityDocumentFile,
                teacherId,
                "identity");

            // Save identity document metadata
            var identityDoc = new TeacherDocument
            {
                TeacherId = teacherId,
                DocumentType = TeacherDocumentType.IdentityDocument,
                FilePath = identityFilePath,
                DocumentNumber = request.DocumentNumber,
                IdentityType = request.IdentityType,
                IssuingCountryCode = request.IssuingCountryCode,
                VerificationStatus = DocumentVerificationStatus.Pending
            };

            await _documentRepository.AddAsync(identityDoc);

            // Validate and save certificates
            foreach (var cert in request.Certificates)
            {
                var isCertValid = await _fileStorageService.ValidateFileAsync(
                    cert.File,
                    allowedExtensions,
                    maxSizeBytes);

                if (!isCertValid)
                {
                    return BadRequest<string>($"Certificate file '{cert.File.FileName}' is invalid or too large");
                }

                var certFilePath = await _fileStorageService.SaveTeacherDocumentAsync(
                    cert.File,
                    teacherId,
                    "certificates");

                var certificate = new TeacherDocument
                {
                    TeacherId = teacherId,
                    DocumentType = TeacherDocumentType.Certificate,
                    FilePath = certFilePath,
                    CertificateTitle = cert.Title,
                    Issuer = cert.Issuer,
                    IssueDate = cert.IssueDate,
                    VerificationStatus = DocumentVerificationStatus.Pending
                };

                await _documentRepository.AddAsync(certificate);
            }

            // Save all documents to database
            await _documentRepository.SaveChangesAsync();

            // Update teacher status and location
            await _teacherRegistrationService.CompleteDocumentUploadAsync(
                teacherId,
                request.IsInSaudiArabia);

            return Success<string>("Registration completed successfully. Your documents are pending verification.");
        }
        catch (Exception ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }
}
