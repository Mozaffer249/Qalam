using Qalam.Data.Helpers;
using Qalam.Service.Email;
using Qalam.Service.Models;

namespace Qalam.Service.Tests;

public class LoginOtpEmailTemplateTests
{
    [Fact]
    public void BuildSubject_IncludesBrandAndBilingualLabels()
    {
        var subject = LoginOtpEmailTemplate.BuildSubject();
        Assert.Contains("Qalam Learning Platform", subject);
        Assert.Contains("verification code", subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("رمز تحقق", subject);
    }

    [Fact]
    public void BuildHtmlBody_IncludesOtpAndBothLanguages()
    {
        var html = LoginOtpEmailTemplate.BuildHtmlBody("1234", 5);
        Assert.Contains("1234", html);
        Assert.Contains("Your verification code", html);
        Assert.Contains("رمز التحقق", html);
        Assert.Contains("dir=\"rtl\"", html);
    }

    [Fact]
    public void BuildHtmlBody_Teacher_MentionsTeacherApp()
    {
        var html = LoginOtpEmailTemplate.BuildHtmlBody("1234", 5, LoginOtpPersona.Teacher);
        Assert.Contains("teacher app", html);
        Assert.Contains("تطبيق المعلم", html);
    }

    [Fact]
    public void BuildHtmlBody_Student_MentionsStudentApp()
    {
        var html = LoginOtpEmailTemplate.BuildHtmlBody("1234", 5, LoginOtpPersona.Student);
        Assert.Contains("student and parent app", html);
        Assert.Contains("الطالب وولي الأمر", html);
    }

    [Fact]
    public void ResolveSecureSocketMode_Port465_UsesSslOnConnect()
    {
        var mode = EmailSecureSocketModeResolver.Resolve(465, true, EmailSecureSocketMode.Auto);
        Assert.Equal(EmailSecureSocketMode.SslOnConnect, mode);
    }
}
