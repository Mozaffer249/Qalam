using FluentValidation.TestHelper;
using Microsoft.Extensions.Localization;
using Moq;
using Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;
using Qalam.Core.Resources.Authentication;
using Xunit;

namespace Qalam.Service.Tests;

public class SubmitTeacherRegistrationRequirementsCommandValidatorTests
{
    private static SubmitTeacherRegistrationRequirementsCommandValidator CreateValidator()
    {
        var localizer = new Mock<IStringLocalizer<AuthenticationResources>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        return new SubmitTeacherRegistrationRequirementsCommandValidator(localizer.Object);
    }

    [Fact]
    public void DocumentNumber_NotRequired_WhenNoIdentityFile()
    {
        var validator = CreateValidator();
        var command = new SubmitTeacherRegistrationRequirementsCommand
        {
            DocumentNumber = null,
            IdentityDocumentFile = null,
        };

        var result = validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.DocumentNumber);
    }

    [Fact]
    public void DocumentNumber_Required_WhenIdentityFilePresent()
    {
        var validator = CreateValidator();
        var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        var command = new SubmitTeacherRegistrationRequirementsCommand
        {
            DocumentNumber = null,
            IdentityDocumentFile = file.Object,
        };

        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DocumentNumber);
    }
}
