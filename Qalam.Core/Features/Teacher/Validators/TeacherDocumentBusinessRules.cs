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
    /// <summary>
    /// Validates Saudi Arabia identity rules based on location
    /// </summary>
    public static void ValidateSaudiIdentityRules(
        bool isInSaudiArabia,
        IdentityType type,
        string? countryCode,
        IStringLocalizer<AuthenticationResources> localizer)
    {
        if (isInSaudiArabia && (type == IdentityType.Passport || type == IdentityType.DrivingLicense))
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.PassportNotAllowedInsideSaudi]);
        }

        if (!isInSaudiArabia && type != IdentityType.Passport && type != IdentityType.DrivingLicense)
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.MustUsePassportOutsideSaudi]);
        }

        if (type == IdentityType.Passport && string.IsNullOrEmpty(countryCode))
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
        IStringLocalizer<AuthenticationResources> localizer)
    {
        var isUnique = await repo.IsIdentityNumberUniqueAsync(type, number, countryCode);

        if (!isUnique)
        {
            throw new ValidationException(
                localizer[AuthenticationResourcesKeys.IdentityDocumentAlreadyRegistered]);
        }
    }
}
