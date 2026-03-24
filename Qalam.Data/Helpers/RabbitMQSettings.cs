namespace Qalam.Data.Helpers
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string EmailQueueName { get; set; } = "email-queue";
        public string SmsQueueName { get; set; } = "sms-queue";
        public string PushQueueName { get; set; } = "push-queue";
        public string FileUploadQueueName { get; set; } = "file-upload-queue";
    }
}
