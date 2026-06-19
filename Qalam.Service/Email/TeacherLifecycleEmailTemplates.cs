using System.Net;

namespace Qalam.Service.Email;

public static class TeacherLifecycleEmailTemplates
{
    public const string BrandName = "Qalam Learning Platform";
    public const string DefaultLoginUrl = "https://qalam.net.sa/";

    public static string BuildRegistrationReceivedSubject() =>
        $"{BrandName} | Application received | تم استلام طلبك";

    public static string BuildDocumentRejectedSubject(string documentLabel) =>
        $"{BrandName} | Document rejected: {documentLabel} | تم رفض مستند";

    public static string BuildSubjectRejectedSubject(string subjectName) =>
        $"{BrandName} | Subject rejected: {subjectName} | تم رفض مادة";

    public static string BuildAccountActivatedSubject() =>
        $"{BrandName} | Your teacher account is active | تم تفعيل حسابك";

    public static string BuildAccountBlockedSubject() =>
        $"{BrandName} | Account suspended | تم إيقاف حسابك";

    public static string BuildAccountUnblockedSubject() =>
        $"{BrandName} | Account restored | تم استعادة حسابك";

    public static string BuildRegistrationReceivedHtml(string loginUrl) =>
        BuildEmailShell(
            titleEn: "Application received",
            titleAr: "تم استلام طلبك",
            bodyEn: """
                <p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#444;">
                  Thank you for submitting your teacher registration on Qalam Learning Platform.
                  Our team is reviewing your documents. You will be notified when there is an update.
                </p>
                """,
            bodyAr: """
                <p style="margin:0 0 16px;font-size:15px;line-height:1.8;color:#444;text-align:right;">
                  شكراً لتقديم طلب التسجيل كمعلم على منصة قلم التعليمية.
                  يقوم فريقنا بمراجعة مستنداتك، وسنُبلغك عند وجود أي تحديث.
                </p>
                """,
            loginUrl,
            hintEn: "Sign in to track your application and add teaching subjects.",
            hintAr: "سجّل الدخول لمتابعة طلبك وإضافة المواد.",
            includeLoginCta: true);

    public static string BuildDocumentRejectedHtml(string loginUrl, string documentLabel, string reason) =>
        BuildEmailShell(
            titleEn: "Document rejected",
            titleAr: "تم رفض مستند",
            bodyEn: $"""
                <p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#444;">
                  Your document <strong>{WebUtility.HtmlEncode(documentLabel)}</strong> was rejected during review.
                </p>
                <p style="margin:0 0 16px;font-size:14px;line-height:1.6;color:#555;background:#fff5f5;border-left:4px solid #dc3545;padding:12px 16px;border-radius:4px;">
                  <strong>Reason:</strong> {WebUtility.HtmlEncode(reason)}
                </p>
                """,
            bodyAr: $"""
                <p style="margin:0 0 16px;font-size:15px;line-height:1.8;color:#444;text-align:right;">
                  تم رفض مستند <strong>{WebUtility.HtmlEncode(documentLabel)}</strong> أثناء المراجعة.
                </p>
                <p style="margin:0 0 16px;font-size:14px;line-height:1.8;color:#555;background:#fff5f5;border-right:4px solid #dc3545;padding:12px 16px;border-radius:4px;text-align:right;">
                  <strong>السبب:</strong> {WebUtility.HtmlEncode(reason)}
                </p>
                """,
            loginUrl,
            hintEn: "Sign in to re-upload the rejected document.",
            hintAr: "سجّل الدخول لإعادة رفع المستند المرفوض.",
            includeLoginCta: true);

    public static string BuildSubjectRejectedHtml(string loginUrl, string subjectName, string reason) =>
        BuildEmailShell(
            titleEn: "Teaching subject rejected",
            titleAr: "تم رفض مادة تعليمية",
            bodyEn: $"""
                <p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#444;">
                  Your teaching subject <strong>{WebUtility.HtmlEncode(subjectName)}</strong> was rejected during review.
                </p>
                <p style="margin:0 0 16px;font-size:14px;line-height:1.6;color:#555;background:#fff5f5;border-left:4px solid #dc3545;padding:12px 16px;border-radius:4px;">
                  <strong>Reason:</strong> {WebUtility.HtmlEncode(reason)}
                </p>
                """,
            bodyAr: $"""
                <p style="margin:0 0 16px;font-size:15px;line-height:1.8;color:#444;text-align:right;">
                  تم رفض المادة التعليمية <strong>{WebUtility.HtmlEncode(subjectName)}</strong> أثناء المراجعة.
                </p>
                <p style="margin:0 0 16px;font-size:14px;line-height:1.8;color:#555;background:#fff5f5;border-right:4px solid #dc3545;padding:12px 16px;border-radius:4px;text-align:right;">
                  <strong>السبب:</strong> {WebUtility.HtmlEncode(reason)}
                </p>
                """,
            loginUrl,
            hintEn: "Sign in to update your teaching subjects.",
            hintAr: "سجّل الدخول لتحديث المواد التعليمية.",
            includeLoginCta: true);

    public static string BuildAccountActivatedHtml(string loginUrl) =>
        BuildEmailShell(
            titleEn: "Your account is active",
            titleAr: "تم تفعيل حسابك",
            bodyEn: """
                <p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#444;">
                  Congratulations! Your teacher account on Qalam Learning Platform has been approved and activated.
                  You can now set your availability and start accepting students.
                </p>
                """,
            bodyAr: """
                <p style="margin:0 0 16px;font-size:15px;line-height:1.8;color:#444;text-align:right;">
                  تهانينا! تمت الموافقة على حسابك كمعلم على منصة قلم التعليمية وتفعيله.
                  يمكنك الآن تحديد أوقاتك والبدء باستقبال الطلاب.
                </p>
                """,
            loginUrl,
            hintEn: "Sign in to set your availability and start teaching.",
            hintAr: "سجّل الدخول لتحديد أوقاتك والبدء بالتدريس.",
            includeLoginCta: true);

    public static string BuildAccountBlockedHtml(string? reason) =>
        BuildEmailShell(
            titleEn: "Account suspended",
            titleAr: "تم إيقاف حسابك",
            bodyEn: BuildBlockedBodyEn(reason),
            bodyAr: BuildBlockedBodyAr(reason),
            loginUrl: DefaultLoginUrl,
            hintEn: string.Empty,
            hintAr: string.Empty,
            includeLoginCta: false);

    public static string BuildAccountUnblockedHtml(string loginUrl) =>
        BuildEmailShell(
            titleEn: "Account restored",
            titleAr: "تم استعادة حسابك",
            bodyEn: """
                <p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#444;">
                  Your teacher account on Qalam Learning Platform has been restored. You can sign in again.
                </p>
                """,
            bodyAr: """
                <p style="margin:0 0 16px;font-size:15px;line-height:1.8;color:#444;text-align:right;">
                  تمت استعادة حسابك كمعلم على منصة قلم التعليمية. يمكنك تسجيل الدخول مجدداً.
                </p>
                """,
            loginUrl,
            hintEn: "Sign in to access your teacher account.",
            hintAr: "سجّل الدخول للوصول إلى حسابك.",
            includeLoginCta: true);

    public static string BuildLoginCtaHtml(string loginUrl, string hintEn, string hintAr)
    {
        var safeUrl = WebUtility.HtmlEncode(loginUrl);
        return $"""
            <p style="margin:0 0 16px;font-size:14px;line-height:1.6;color:#555;">{WebUtility.HtmlEncode(hintEn)}</p>
            <table role="presentation" cellspacing="0" cellpadding="0" style="margin:0 0 8px;">
              <tr>
                <td align="center" style="border-radius:8px;background:#007bff;">
                  <a href="{safeUrl}" target="_blank" rel="noopener noreferrer"
                     style="display:inline-block;padding:14px 28px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:8px;">
                    Log in to Qalam
                  </a>
                </td>
              </tr>
            </table>
            <p style="margin:0;font-size:13px;color:#888;">
              Or visit: <a href="{safeUrl}" style="color:#007bff;">{safeUrl}</a>
            </p>
            <div dir="rtl" style="margin-top:20px;border-top:1px solid #e8eef4;padding-top:16px;">
              <p style="margin:0 0 16px;font-size:14px;line-height:1.8;color:#555;text-align:right;">{WebUtility.HtmlEncode(hintAr)}</p>
              <table role="presentation" cellspacing="0" cellpadding="0" style="margin:0 auto 8px;">
                <tr>
                  <td align="center" style="border-radius:8px;background:#007bff;">
                    <a href="{safeUrl}" target="_blank" rel="noopener noreferrer"
                       style="display:inline-block;padding:14px 28px;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:8px;">
                      تسجيل الدخول إلى قلم
                    </a>
                  </td>
                </tr>
              </table>
            </div>
            """;
    }

    private static string BuildBlockedBodyEn(string? reason)
    {
        var reasonBlock = string.IsNullOrWhiteSpace(reason)
            ? string.Empty
            : $"""
              <p style="margin:0 0 16px;font-size:14px;line-height:1.6;color:#555;background:#fff5f5;border-left:4px solid #dc3545;padding:12px 16px;border-radius:4px;">
                <strong>Reason:</strong> {WebUtility.HtmlEncode(reason)}
              </p>
              """;
        return $"""
            <p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:#444;">
              Your teacher account on Qalam Learning Platform has been suspended by an administrator.
              You cannot sign in until your account is restored.
            </p>
            {reasonBlock}
            <p style="margin:0;font-size:14px;line-height:1.6;color:#555;">
              If you believe this is an error, please contact support.
            </p>
            """;
    }

    private static string BuildBlockedBodyAr(string? reason)
    {
        var reasonBlock = string.IsNullOrWhiteSpace(reason)
            ? string.Empty
            : $"""
              <p style="margin:0 0 16px;font-size:14px;line-height:1.8;color:#555;background:#fff5f5;border-right:4px solid #dc3545;padding:12px 16px;border-radius:4px;text-align:right;">
                <strong>السبب:</strong> {WebUtility.HtmlEncode(reason)}
              </p>
              """;
        return $"""
            <p style="margin:0 0 16px;font-size:15px;line-height:1.8;color:#444;text-align:right;">
              تم إيقاف حسابك كمعلم على منصة قلم التعليمية من قبل الإدارة.
              لا يمكنك تسجيل الدخول حتى يتم استعادة حسابك.
            </p>
            {reasonBlock}
            <p style="margin:0;font-size:14px;line-height:1.8;color:#555;text-align:right;">
              إذا كنت تعتقد أن هذا خطأ، يرجى التواصل مع الدعم.
            </p>
            """;
    }

    private static string BuildEmailShell(
        string titleEn,
        string titleAr,
        string bodyEn,
        string bodyAr,
        string loginUrl,
        string hintEn,
        string hintAr,
        bool includeLoginCta)
    {
        var ctaEn = includeLoginCta ? BuildLoginCtaHtml(loginUrl, hintEn, hintAr) : string.Empty;
        return $"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>{BrandName}</title>
</head>
<body style="margin:0;padding:0;background-color:#f0f4f8;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f0f4f8;padding:32px 16px;">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,86,179,0.12);">
          <tr>
            <td style="background:linear-gradient(135deg,#007bff 0%,#0056b3 100%);padding:28px 24px;text-align:center;">
              <p style="margin:0 0 6px;font-size:13px;letter-spacing:0.08em;text-transform:uppercase;color:rgba(255,255,255,0.85);">Teacher · معلم</p>
              <h1 style="margin:0;font-size:22px;font-weight:700;color:#ffffff;line-height:1.3;">{BrandName}</h1>
            </td>
          </tr>
          <tr>
            <td style="padding:32px 28px 24px;" dir="ltr">
              <h2 style="margin:0 0 12px;font-size:20px;color:#0056b3;">{WebUtility.HtmlEncode(titleEn)}</h2>
              {bodyEn}
              {(includeLoginCta ? ctaEn : string.Empty)}
            </td>
          </tr>
          <tr>
            <td style="padding:0 28px 28px;" dir="rtl">
              <div style="border-top:1px solid #e8eef4;padding-top:24px;">
                <h2 style="margin:0 0 12px;font-size:20px;color:#0056b3;text-align:right;">{WebUtility.HtmlEncode(titleAr)}</h2>
                {bodyAr}
              </div>
            </td>
          </tr>
          <tr>
            <td style="background:#f8fafc;padding:20px 28px;border-top:1px solid #e8eef4;">
              <p style="margin:0;font-size:11px;color:#aaa;text-align:center;">
                © {DateTime.UtcNow.Year} {BrandName} · Automated message
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
    }
}
