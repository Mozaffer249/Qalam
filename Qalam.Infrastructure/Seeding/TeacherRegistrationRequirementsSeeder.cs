using Microsoft.EntityFrameworkCore;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Seeding;

public static class TeacherRegistrationRequirementsSeeder
{
    public static async Task SeedAsync(ApplicationDBContext context)
    {
        if (!await SeederHelper.TableExistsAsync(context, "teacher", "TeacherRegistrationRequirements"))
            return;

        var now = DateTime.UtcNow;
        var seeds = TeacherRegistrationRequirementsDefaults.Create(now);

        foreach (var seed in seeds)
        {
            var exists = await context.TeacherRegistrationRequirements
                .AnyAsync(r => r.Code == seed.Code);
            if (!exists)
                await context.TeacherRegistrationRequirements.AddAsync(seed);
        }

        // Deactivate legacy "موقع التدريس" — nationality now drives identity options + Teacher.Location.
        var locationRows = await context.TeacherRegistrationRequirements
            .Where(r => r.Code == TeacherRegistrationRequirementCodes.Location && r.IsActive)
            .ToListAsync();
        foreach (var row in locationRows)
        {
            row.IsActive = false;
            row.IsRequired = false;
            row.UpdatedAt = now;
        }

        await context.SaveChangesAsync();
        await BackfillSubmissionsFromDocumentsAsync(context);
    }

    /// <summary>
    /// Links existing TeacherDocuments to submissions for teachers already in verification flow.
    /// </summary>
    private static async Task BackfillSubmissionsFromDocumentsAsync(ApplicationDBContext context)
    {
        var requirements = await context.TeacherRegistrationRequirements.AsNoTracking().ToListAsync();
        var identityReq = requirements.FirstOrDefault(r => r.Code == TeacherRegistrationRequirementCodes.IdentityDocument);
        var certReq = requirements.FirstOrDefault(r => r.Code == TeacherRegistrationRequirementCodes.Certificate);
        if (identityReq == null && certReq == null)
            return;

        var teachersWithDocs = await context.TeacherDocuments
            .AsNoTracking()
            .Select(d => d.TeacherId)
            .Distinct()
            .ToListAsync();

        foreach (var teacherId in teachersWithDocs)
        {
            var docs = await context.TeacherDocuments
                .Where(d => d.TeacherId == teacherId)
                .ToListAsync();

            foreach (var doc in docs)
            {
                TeacherRegistrationRequirement? req = doc.DocumentType switch
                {
                    TeacherDocumentType.IdentityDocument => identityReq,
                    TeacherDocumentType.Certificate => certReq,
                    _ => null
                };
                if (req == null)
                    continue;

                var hasSubmission = await context.TeacherRegistrationSubmissions
                    .AnyAsync(s => s.TeacherId == teacherId && s.RequirementId == req.Id && s.TeacherDocumentId == doc.Id);
                if (hasSubmission)
                    continue;

                var existingForReq = await context.TeacherRegistrationSubmissions
                    .FirstOrDefaultAsync(s => s.TeacherId == teacherId && s.RequirementId == req.Id && s.TeacherDocumentId == null);

                if (doc.DocumentType == TeacherDocumentType.IdentityDocument)
                {
                    if (existingForReq != null)
                    {
                        existingForReq.TeacherDocumentId = doc.Id;
                        existingForReq.VerificationStatus = doc.VerificationStatus;
                        existingForReq.ReviewedAt = doc.ReviewedAt;
                        existingForReq.ReviewedByAdminId = doc.ReviewedByAdminId;
                        existingForReq.RejectionReason = doc.RejectionReason;
                    }
                    else
                    {
                        await context.TeacherRegistrationSubmissions.AddAsync(new TeacherRegistrationSubmission
                        {
                            TeacherId = teacherId,
                            RequirementId = req.Id,
                            TeacherDocumentId = doc.Id,
                            VerificationStatus = doc.VerificationStatus,
                            ReviewedAt = doc.ReviewedAt,
                            ReviewedByAdminId = doc.ReviewedByAdminId,
                            RejectionReason = doc.RejectionReason,
                            CreatedAt = doc.CreatedAt
                        });
                    }
                }
                else if (doc.DocumentType == TeacherDocumentType.Certificate)
                {
                    await context.TeacherRegistrationSubmissions.AddAsync(new TeacherRegistrationSubmission
                    {
                        TeacherId = teacherId,
                        RequirementId = req.Id,
                        TeacherDocumentId = doc.Id,
                        VerificationStatus = doc.VerificationStatus,
                        ReviewedAt = doc.ReviewedAt,
                        ReviewedByAdminId = doc.ReviewedByAdminId,
                        RejectionReason = doc.RejectionReason,
                        CreatedAt = doc.CreatedAt
                    });
                }
            }

            var teacher = await context.Teachers.FindAsync(teacherId);
            if (teacher != null && teacher.Location.HasValue)
            {
                var locReq = requirements.FirstOrDefault(r => r.Code == TeacherRegistrationRequirementCodes.Location);
                if (locReq != null && !await context.TeacherRegistrationSubmissions
                        .AnyAsync(s => s.TeacherId == teacherId && s.RequirementId == locReq.Id))
                {
                    await context.TeacherRegistrationSubmissions.AddAsync(new TeacherRegistrationSubmission
                    {
                        TeacherId = teacherId,
                        RequirementId = locReq.Id,
                        BoolValue = teacher.Location == TeacherLocation.InsideSaudiArabia,
                        VerificationStatus = DocumentVerificationStatus.Approved,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (teacher != null && !string.IsNullOrWhiteSpace(teacher.Bio))
            {
                var bioReq = requirements.FirstOrDefault(r => r.Code == TeacherRegistrationRequirementCodes.Bio);
                if (bioReq != null && !await context.TeacherRegistrationSubmissions
                        .AnyAsync(s => s.TeacherId == teacherId && s.RequirementId == bioReq.Id))
                {
                    await context.TeacherRegistrationSubmissions.AddAsync(new TeacherRegistrationSubmission
                    {
                        TeacherId = teacherId,
                        RequirementId = bioReq.Id,
                        TextValue = teacher.Bio,
                        VerificationStatus = DocumentVerificationStatus.Approved,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
