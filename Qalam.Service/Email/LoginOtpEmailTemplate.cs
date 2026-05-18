using Qalam.Service.Models;

namespace Qalam.Service.Email;

public static class LoginOtpEmailTemplate
{
  public const string BrandName = "Qalam Learning Platform";

  public static string BuildSubject(LoginOtpPersona persona = LoginOtpPersona.Teacher)
  {
    var roleEn = persona == LoginOtpPersona.Teacher ? "Teacher" : "Student";
    var roleAr = persona == LoginOtpPersona.Teacher ? "المعلم" : "الطالب";
    return $"{BrandName} | {roleEn} verification code | رمز تحقق {roleAr}";
  }

  public static string BuildHtmlBody(string otpCode, int expiryMinutes, LoginOtpPersona persona = LoginOtpPersona.Teacher)
  {
    var expiryEn = expiryMinutes == 1 ? "1 minute" : $"{expiryMinutes} minutes";
    var expiryAr = expiryMinutes == 1 ? "دقيقة واحدة" : $"{expiryMinutes} دقائق";

    var (introEn, introAr, badgeEn, badgeAr) = persona switch
    {
      LoginOtpPersona.Student => (
        "Use the code below to sign in to the student and parent app on Qalam Learning Platform.",
        "استخدم الرمز أدناه لتسجيل الدخول إلى تطبيق الطالب وولي الأمر على منصة قلم التعليمية.",
        "Student · Parent",
        "طالب · ولي أمر"),
      _ => (
        "Use the code below to sign in to the teacher app on Qalam Learning Platform.",
        "استخدم الرمز أدناه لتسجيل الدخول إلى تطبيق المعلم على منصة قلم التعليمية.",
        "Teacher",
        "معلم")
    };

    return $"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>{BrandName} — Verification code</title>
</head>
<body style="margin:0;padding:0;background-color:#f0f4f8;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f0f4f8;padding:32px 16px;">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,86,179,0.12);">
          <tr>
            <td style="background:linear-gradient(135deg,#007bff 0%,#0056b3 100%);padding:28px 24px;text-align:center;">
              <p style="margin:0 0 6px;font-size:13px;letter-spacing:0.08em;text-transform:uppercase;color:rgba(255,255,255,0.85);">{badgeEn} · {badgeAr}</p>
              <h1 style="margin:0;font-size:22px;font-weight:700;color:#ffffff;line-height:1.3;">{BrandName}</h1>
            </td>
          </tr>
          <tr>
            <td style="padding:32px 28px 24px;" dir="ltr">
              <h2 style="margin:0 0 12px;font-size:20px;color:#0056b3;">Your verification code</h2>
              <p style="margin:0 0 20px;font-size:15px;line-height:1.6;color:#444;">
                {introEn} Do not share it with anyone.
              </p>
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                <tr>
                  <td align="center" style="background:#f8fafc;border:2px dashed #007bff;border-radius:10px;padding:20px;">
                    <span style="font-size:36px;font-weight:700;letter-spacing:0.35em;color:#0056b3;font-family:Consolas,'Courier New',monospace;">{otpCode}</span>
                  </td>
                </tr>
              </table>
              <p style="margin:20px 0 0;font-size:14px;color:#666;">Valid for <strong>{expiryEn}</strong>.</p>
            </td>
          </tr>
          <tr>
            <td style="padding:0 28px 28px;" dir="rtl">
              <div style="border-top:1px solid #e8eef4;padding-top:24px;">
                <h2 style="margin:0 0 12px;font-size:20px;color:#0056b3;text-align:right;">رمز التحقق</h2>
                <p style="margin:0 0 20px;font-size:15px;line-height:1.8;color:#444;text-align:right;">
                  {introAr} لا تشاركه مع أي شخص.
                </p>
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                  <tr>
                    <td align="center" style="background:#f8fafc;border:2px dashed #007bff;border-radius:10px;padding:20px;">
                      <span style="font-size:36px;font-weight:700;letter-spacing:0.35em;color:#0056b3;font-family:Consolas,'Courier New',monospace;">{otpCode}</span>
                    </td>
                  </tr>
                </table>
                <p style="margin:20px 0 0;font-size:14px;color:#666;text-align:right;">صالح لمدة <strong>{expiryAr}</strong>.</p>
              </div>
            </td>
          </tr>
          <tr>
            <td style="background:#f8fafc;padding:20px 28px;border-top:1px solid #e8eef4;">
              <p style="margin:0 0 8px;font-size:12px;line-height:1.5;color:#888;text-align:center;" dir="ltr">
                If you did not request this code, you can safely ignore this email.
              </p>
              <p style="margin:0;font-size:12px;line-height:1.5;color:#888;text-align:center;" dir="rtl">
                إذا لم تطلب هذا الرمز، يمكنك تجاهل هذه الرسالة بأمان.
              </p>
              <p style="margin:16px 0 0;font-size:11px;color:#aaa;text-align:center;">
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
