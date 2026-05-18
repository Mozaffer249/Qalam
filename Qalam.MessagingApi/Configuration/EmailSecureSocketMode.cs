namespace Qalam.MessagingApi.Configuration;

public enum EmailSecureSocketMode
{
  Auto = 0,
  None = 1,
  StartTls = 2,
  SslOnConnect = 3
}

public static class EmailSecureSocketModeResolver
{
  public static EmailSecureSocketMode Resolve(int port, bool enableSsl, EmailSecureSocketMode configured) =>
      configured switch
      {
        EmailSecureSocketMode.Auto when port == 465 => EmailSecureSocketMode.SslOnConnect,
        EmailSecureSocketMode.Auto when enableSsl => EmailSecureSocketMode.StartTls,
        EmailSecureSocketMode.Auto => EmailSecureSocketMode.None,
        _ => configured
      };
}
