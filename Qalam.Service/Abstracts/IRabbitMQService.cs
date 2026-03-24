using Qalam.Data.Entity.Messaging;

namespace Qalam.Service.Abstracts
{
    public interface IRabbitMQService
    {
        Task QueueEmailAsync(EmailMessage emailMessage);
        Task QueueSmsAsync(SmsMessage smsMessage);
        Task QueuePushNotificationAsync(PushNotificationMessage pushMessage);
        Task QueueFileUploadAsync(FileUploadMessage fileUploadMessage);
    }
}
