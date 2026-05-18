namespace Qalam.Data.Helpers;

public enum EmailSecureSocketMode
{
  /// <summary>Infer from port: 465 → SslOnConnect, else StartTls when EnableSsl.</summary>
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
