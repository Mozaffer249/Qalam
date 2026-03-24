namespace Qalam.MessagingApi.Configuration;

public class WasabiSettings
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "qalam-storage";
    public string Region { get; set; } = "eu-central-1";
    public string ServiceUrl { get; set; } = "https://s3.eu-central-1.wasabisys.com";
}
