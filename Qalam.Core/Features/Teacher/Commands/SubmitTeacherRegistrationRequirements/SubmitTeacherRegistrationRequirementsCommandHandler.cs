using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Teacher.Validators;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;

public class SubmitTeacherRegistrationRequirementsCommandHandler : ResponseHandler,
    IRequestHandler<SubmitTeacherRegistrationRequirementsCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRegistrationRequirementRepository _requirementRepository;
    private readonly ITeacherRegistrationSubmissionRepository _submissionRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITeacherRegistrationService _teacherRegistrationService;
    private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

    public SubmitTeacherRegistrationRequirementsCommandHandler(
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRegistrationRequirementRepository requirementRepository,
        ITeacherRegistrationSubmissionRepository submissionRepository,
        IFileStorageService fileStorageService,
        ITeacherRegistrationService teacherRegistrationService,
        IStringLocalizer<SharedResources> sharedLocalizer,
        IStringLocalizer<AuthenticationResources> authLocalizer) : base(sharedLocalizer)
    {
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
        _requirementRepository = requirementRepository;
        _submissionRepository = submissionRepository;
        _fileStorageService = fileStorageService;
        _teacherRegistrationService = teacherRegistrationService;
        _authLocalizer = authLocalizer;
    }

    public async Task<Response<string>> Handle(
        SubmitTeacherRegistrationRequirementsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == 0)
                return Unauthorized<string>("User not authenticated");

            var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
            if (teacher == null)
                return BadRequest<string>("Teacher profile not found. Please complete personal information first.");

            if (teacher.Status == TeacherStatus.PendingVerification)
                return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.DocumentsAlreadyPendingVerification]);
            if (teacher.Status == TeacherStatus.Active)
                return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.AccountAlreadyVerified]);
            if (teacher.Status == TeacherStatus.Blocked)
                return Unauthorized<string>(_authLocalizer[AuthenticationResourcesKeys.AccountBlocked]);

            var activeRequirements = await _requirementRepository.GetActiveOrderedAsync(cancellationToken);
            if (activeRequirements.Count == 0)
                return BadRequest<string>("No active registration requirements configured.");

            var validationError = ValidateAgainstRequirements(request, activeRequirements);
            if (validationError != null)
                return BadRequest<string>(validationError);

            var teacherId = teacher.Id;
            var defaultExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            const int defaultMaxSize = 10 * 1024 * 1024;

            foreach (var req in activeRequirements)
            {
                switch (req.RequirementType)
                {
                    case RegistrationRequirementType.Boolean when req.Code == TeacherRegistrationRequirementCodes.Location:
                        teacher.Location = request.IsInSaudiArabia
                            ? TeacherLocation.InsideSaudiArabia
                            : TeacherLocation.OutsideSaudiArabia;
                        await UpsertSubmissionAsync(teacherId, req.Id, boolValue: request.IsInSaudiArabia,
                            status: DocumentVerificationStatus.Approved, cancellationToken: cancellationToken);
                        break;

                    case RegistrationRequirementType.Text when req.Code == TeacherRegistrationRequirementCodes.Bio:
                        if (!string.IsNullOrWhiteSpace(request.Bio))
                        {
                            if (req.MaxLength.HasValue && request.Bio.Length > req.MaxLength.Value)
                                return BadRequest<string>($"Bio exceeds maximum length of {req.MaxLength.Value} characters.");
                            teacher.Bio = request.Bio.Trim();
                            await UpsertSubmissionAsync(teacherId, req.Id, textValue: teacher.Bio,
                                status: DocumentVerificationStatus.Approved, cancellationToken: cancellationToken);
                        }
                        break;

                    case RegistrationRequirementType.File when req.Code == TeacherRegistrationRequirementCodes.IdentityDocument:
                        await ProcessIdentityAsync(request, teacherId, req, defaultExtensions, defaultMaxSize, cancellationToken);
                        break;

                    case RegistrationRequirementType.File when req.Code == TeacherRegistrationRequirementCodes.Certificate:
                        await ProcessCertificatesAsync(request, teacherId, req, defaultExtensions, defaultMaxSize, cancellationToken);
                        break;

                    case RegistrationRequirementType.File:
                        if (request.CustomFilesByCode.TryGetValue(req.Code, out var customFiles))
                            await ProcessCustomFilesAsync(teacherId, req, customFiles, cancellationToken);
                        break;
                }
            }

            await _teacherRepository.UpdateAsync(teacher);
            await _teacherRepository.SaveChangesAsync();

            await _teacherRegistrationService.CompleteDocumentUploadAsync(teacherId, request.IsInSaudiArabia);

            return Success<string>("Registration submitted successfully. Your information is pending verification.");
        }
        catch (Exception ex)
        {
            return BadRequest<string>(ex.Message);
        }
    }

    private static string? ValidateAgainstRequirements(
        SubmitTeacherRegistrationRequirementsCommand request,
        List<TeacherRegistrationRequirement> active)
    {
        foreach (var req in active.Where(r => r.IsRequired))
        {
            switch (req.RequirementType)
            {
                case RegistrationRequirementType.File when req.Code == TeacherRegistrationRequirementCodes.IdentityDocument:
                    if (request.IdentityDocumentFile == null)
                        return "Identity document is required.";
                    break;
                case RegistrationRequirementType.File when req.Code == TeacherRegistrationRequirementCodes.Certificate:
                    if (request.Certificates.Count < req.MinCount)
                        return $"At least {req.MinCount} certificate(s) required.";
                    break;
                case RegistrationRequirementType.File:
                    if (!request.CustomFilesByCode.TryGetValue(req.Code, out var files) || files.Count < req.MinCount)
                        return $"Requirement '{req.Code}' requires at least {req.MinCount} file(s).";
                    break;
                case RegistrationRequirementType.Text when req.Code == TeacherRegistrationRequirementCodes.Bio:
                    if (string.IsNullOrWhiteSpace(request.Bio))
                        return "Bio is required.";
                    break;
            }
        }

        var certReq = active.FirstOrDefault(r => r.Code == TeacherRegistrationRequirementCodes.Certificate);
        if (certReq != null && request.Certificates.Count > certReq.MaxCount)
            return $"Maximum {certReq.MaxCount} certificates allowed.";

        return null;
    }

    private async Task ProcessIdentityAsync(
        SubmitTeacherRegistrationRequirementsCommand request,
        int teacherId,
        TeacherRegistrationRequirement req,
        string[] defaultExtensions,
        int defaultMaxSize,
        CancellationToken cancellationToken)
    {
        if (request.IdentityDocumentFile == null)
            return;

        TeacherDocumentBusinessRules.ValidateSaudiIdentityRules(
            request.IsInSaudiArabia, request.IdentityType, request.IssuingCountryCode, _authLocalizer);

        await TeacherDocumentBusinessRules.ValidateIdentityUnique(
            _documentRepository, request.IdentityType, request.DocumentNumber, request.IssuingCountryCode, _authLocalizer);

        var extensions = RegistrationRequirementExtensionsHelper.Parse(req.AllowedExtensionsJson);
        if (extensions.Count == 0) extensions = defaultExtensions.ToList();
        var limit = req.MaxFileSizeBytes > 0 ? req.MaxFileSizeBytes : defaultMaxSize;

        if (!await _fileStorageService.ValidateFileAsync(request.IdentityDocumentFile, extensions.ToArray(), limit))
            throw new InvalidOperationException("Identity document file is invalid or too large");

        var identityDoc = new TeacherDocument
        {
            TeacherId = teacherId,
            DocumentType = TeacherDocumentType.IdentityDocument,
            FilePath = "pending-upload",
            DocumentNumber = request.DocumentNumber,
            IdentityType = request.IdentityType,
            IssuingCountryCode = request.IssuingCountryCode,
            VerificationStatus = DocumentVerificationStatus.Pending
        };

        await _documentRepository.AddAsync(identityDoc);
        await _documentRepository.SaveChangesAsync();

        await _fileStorageService.QueueTeacherDocUploadAsync(
            request.IdentityDocumentFile, teacherId, "identity", identityDoc.Id);

        await UpsertSubmissionAsync(teacherId, req.Id, documentId: identityDoc.Id,
            status: DocumentVerificationStatus.Pending, cancellationToken: cancellationToken);
    }

    private async Task ProcessCertificatesAsync(
        SubmitTeacherRegistrationRequirementsCommand request,
        int teacherId,
        TeacherRegistrationRequirement req,
        string[] defaultExtensions,
        int defaultMaxSize,
        CancellationToken cancellationToken)
    {
        if (request.Certificates.Count == 0)
            return;

        var extensions = RegistrationRequirementExtensionsHelper.Parse(req.AllowedExtensionsJson);
        if (extensions.Count == 0) extensions = defaultExtensions.ToList();
        var limit = req.MaxFileSizeBytes > 0 ? req.MaxFileSizeBytes : defaultMaxSize;

        foreach (var cert in request.Certificates)
        {
            if (!await _fileStorageService.ValidateFileAsync(cert.File, extensions.ToArray(), limit))
                throw new InvalidOperationException($"Certificate file '{cert.File.FileName}' is invalid or too large");

            var certificate = new TeacherDocument
            {
                TeacherId = teacherId,
                DocumentType = TeacherDocumentType.Certificate,
                FilePath = "pending-upload",
                CertificateTitle = cert.Title,
                Issuer = cert.Issuer,
                IssueDate = cert.IssueDate,
                VerificationStatus = DocumentVerificationStatus.Pending
            };

            await _documentRepository.AddAsync(certificate);
            await _documentRepository.SaveChangesAsync();

            await _fileStorageService.QueueTeacherDocUploadAsync(cert.File, teacherId, "certificates", certificate.Id);

            await UpsertSubmissionAsync(teacherId, req.Id, documentId: certificate.Id,
                status: DocumentVerificationStatus.Pending, cancellationToken: cancellationToken);
        }
    }

    private async Task ProcessCustomFilesAsync(
        int teacherId,
        TeacherRegistrationRequirement req,
        List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var extensions = RegistrationRequirementExtensionsHelper.Parse(req.AllowedExtensionsJson);
        var limit = req.MaxFileSizeBytes > 0 ? req.MaxFileSizeBytes : 10 * 1024 * 1024;
        var docType = req.MapsToDocumentType ?? TeacherDocumentType.Other;

        foreach (var file in files)
        {
            if (!await _fileStorageService.ValidateFileAsync(file, extensions.ToArray(), limit))
                throw new InvalidOperationException($"File for '{req.Code}' is invalid or too large");

            var doc = new TeacherDocument
            {
                TeacherId = teacherId,
                DocumentType = docType,
                FilePath = "pending-upload",
                VerificationStatus = DocumentVerificationStatus.Pending
            };

            await _documentRepository.AddAsync(doc);
            await _documentRepository.SaveChangesAsync();

            await _fileStorageService.QueueTeacherDocUploadAsync(file, teacherId, req.Code, doc.Id);

            await UpsertSubmissionAsync(teacherId, req.Id, documentId: doc.Id,
                status: DocumentVerificationStatus.Pending, cancellationToken: cancellationToken);
        }
    }

    private async Task UpsertSubmissionAsync(
        int teacherId,
        int requirementId,
        int? documentId = null,
        string? textValue = null,
        bool? boolValue = null,
        DocumentVerificationStatus status = DocumentVerificationStatus.Pending,
        CancellationToken cancellationToken = default)
    {
        if (documentId.HasValue)
        {
            await _submissionRepository.AddAsync(new TeacherRegistrationSubmission
            {
                TeacherId = teacherId,
                RequirementId = requirementId,
                TeacherDocumentId = documentId,
                VerificationStatus = status
            });
            await _submissionRepository.SaveChangesAsync();
            return;
        }

        var sub = await _submissionRepository.GetByTeacherAndRequirementAsync(teacherId, requirementId, cancellationToken);
        if (sub == null)
        {
            await _submissionRepository.AddAsync(new TeacherRegistrationSubmission
            {
                TeacherId = teacherId,
                RequirementId = requirementId,
                TextValue = textValue,
                BoolValue = boolValue,
                VerificationStatus = status
            });
        }
        else
        {
            sub.TextValue = textValue ?? sub.TextValue;
            sub.BoolValue = boolValue ?? sub.BoolValue;
            sub.VerificationStatus = status;
            sub.RejectionReason = null;
            await _submissionRepository.UpdateAsync(sub);
        }

        await _submissionRepository.SaveChangesAsync();
    }
}
