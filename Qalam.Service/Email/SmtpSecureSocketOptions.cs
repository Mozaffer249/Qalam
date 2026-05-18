using MailKit.Security;
using Qalam.Data.Helpers;

namespace Qalam.Service.Email;

public static class SmtpSecureSocketOptions
{
  public static SecureSocketOptions FromEmailSettings(EmailSettings settings)
  {
    var mode = EmailSecureSocketModeResolver.Resolve(
        settings.Port,
        settings.EnableSsl,
        settings.SecureSocketMode);

    return mode switch
    {
      EmailSecureSocketMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
      EmailSecureSocketMode.StartTls => SecureSocketOptions.StartTls,
      _ => SecureSocketOptions.None
    };
  }
}
