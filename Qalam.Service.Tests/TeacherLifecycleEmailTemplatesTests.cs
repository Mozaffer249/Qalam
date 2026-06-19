using Qalam.Service.Email;
using Xunit;

namespace Qalam.Service.Tests;

public class TeacherLifecycleEmailTemplatesTests
{
    [Fact]
    public void RegistrationEmailGate_OnlyFirstSubmission()
    {
        Assert.True(ShouldSendRegistrationEmail(Qalam.Data.Entity.Common.Enums.TeacherStatus.AwaitingDocuments));
        Assert.False(ShouldSendRegistrationEmail(Qalam.Data.Entity.Common.Enums.TeacherStatus.DocumentsRejected));
        Assert.False(ShouldSendRegistrationEmail(Qalam.Data.Entity.Common.Enums.TeacherStatus.PendingVerification));
    }

    [Fact]
    public void DocumentRejectedHtml_IncludesLoginUrlAndReason()
    {
        var html = TeacherLifecycleEmailTemplates.BuildDocumentRejectedHtml(
            "http://localhost:3000/",
            "Identity document",
            "Blurry scan");

        Assert.Contains("http://localhost:3000/", html);
        Assert.Contains("Blurry scan", html);
        Assert.Contains("Log in to Qalam", html);
    }

    [Fact]
    public void SubjectRejectedHtml_IncludesSubjectName()
    {
        var html = TeacherLifecycleEmailTemplates.BuildSubjectRejectedHtml(
            "https://qalam.net.sa/",
            "Mathematics",
            "Missing certificate");

        Assert.Contains("Mathematics", html);
        Assert.Contains("Missing certificate", html);
    }

    private static bool ShouldSendRegistrationEmail(Qalam.Data.Entity.Common.Enums.TeacherStatus previousStatus) =>
        previousStatus == Qalam.Data.Entity.Common.Enums.TeacherStatus.AwaitingDocuments;
}
