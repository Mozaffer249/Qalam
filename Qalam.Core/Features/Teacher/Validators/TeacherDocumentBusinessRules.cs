using FluentValidation;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Validators;

/// <summary>
/// Business rules for teacher document validation
/// </summary>
public static class TeacherDocumentBusinessRules
{
    /// <summary>
    /// Validates Saudi Arabia identity rules based on location
    /// </summary>
    public static void ValidateSaudiIdentityRules(
        bool isInSaudiArabia,
        IdentityType type,
        string? countryCode)
    {
        if (isInSaudiArabia && (type == IdentityType.Passport || type == IdentityType.DrivingLicense))
        {
            throw new ValidationException(
                "Passport is not allowed for teachers inside Saudi Arabia. Please use National ID or Iqama.");
        }

        if (!isInSaudiArabia && type != IdentityType.Passport && type != IdentityType.DrivingLicense)
        {
            throw new ValidationException(
                "Must use Passport for teachers outside Saudi Arabia.");
        }

        if (type == IdentityType.Passport && string.IsNullOrEmpty(countryCode))
        {
            throw new ValidationException(
                "Issuing country is required for Passport.");
        }

        if ((type == IdentityType.NationalId || type == IdentityType.Iqama)
            && !string.IsNullOrEmpty(countryCode))
        {
            throw new ValidationException(
                "Issuing country should not be provided for National ID or Iqama.");
        }
    }

    /// <summary>
    /// Validates certificate count (min 1, max 5)
    /// </summary>
    public static void ValidateCertificateCount(int count)
    {
        if (count < 1)
        {
            throw new ValidationException("At least 1 certificate is required");
        }

        if (count > 5)
        {
            throw new ValidationException("Maximum 5 certificates allowed");
        }
    }

    /// <summary>
    /// Validates that the identity document number is unique
    /// </summary>
    public static async Task ValidateIdentityUnique(
        ITeacherDocumentRepository repo,
        IdentityType type,
        string number,
        string? countryCode)
    {
        var isUnique = await repo.IsIdentityNumberUniqueAsync(type, number, countryCode);

        if (!isUnique)
        {
            throw new ValidationException(
                "This identity document is already registered in the system.");
        }
    }
}
