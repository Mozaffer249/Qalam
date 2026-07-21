using FluentValidation;
using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.Validators;

/// <summary>
/// Business rules for teacher document validation
/// </summary>
public static class TeacherDocumentBusinessRules
{
    public const string SaudiArabiaCode = "SA";

    public static bool IsSaudiNationality(string? nationalityCode) =>
        string.Equals(nationalityCode?.Trim(), SaudiArabiaCode, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Validates identity rules based on nationality (SA vs non-SA).
    /// For foreign IDs, <paramref name="countryCode"/> should equal the nationality code.
    /// </summary>
    public static void ValidateSaudiIdentityRules(
        string? nationalityCode,
        IdentityType type,
        string? countryCode,
        IStringLocalizer<AuthenticationResources> localizer)
    {
        var isSaudi = IsSaudiNationality(nationalityCode);

        if (isSaudi &&
            (type == IdentityType.Passport
             || type == IdentityType.DrivingLicense
             || type == IdentityType.GovernmentId))
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.PassportNotAllowedInsideSaudi]);
        }

        if (!isSaudi
            && type != IdentityType.Passport
            && type != IdentityType.DrivingLicense
            && type != IdentityType.GovernmentId)
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.MustUsePassportOutsideSaudi]);
        }

        if ((type == IdentityType.Passport
             || type == IdentityType.DrivingLicense
             || type == IdentityType.GovernmentId)
            && string.IsNullOrEmpty(countryCode))
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.IssuingCountryRequiredForPassport]);
        }

        if ((type == IdentityType.NationalId || type == IdentityType.Iqama)
            && !string.IsNullOrEmpty(countryCode))
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.IssuingCountryShouldNotBeProvided]);
        }
    }

    /// <summary>
    /// Validates certificate count (min 1, max 5)
    /// </summary>
    public static void ValidateCertificateCount(int count, IStringLocalizer<AuthenticationResources> localizer)
    {
        if (count < 1)
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.AtLeastOneCertificateRequired]);
        }

        if (count > 5)
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.MaximumFiveCertificatesAllowed]);
        }
    }

    /// <summary>
    /// Validates that the identity document number is unique
    /// </summary>
    public static async Task ValidateIdentityUnique(
        ITeacherDocumentRepository repo,
        IdentityType type,
        string number,
        string? countryCode,
        IStringLocalizer<AuthenticationResources> localizer,
        int? excludeTeacherId = null)
    {
        var isUnique = await repo.IsIdentityNumberUniqueAsync(type, number, countryCode, excludeTeacherId);

        if (!isUnique)
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.IdentityDocumentAlreadyRegistered]);
        }
    }
}
