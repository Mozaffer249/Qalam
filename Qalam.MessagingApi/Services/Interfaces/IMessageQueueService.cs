using Qalam.MessagingApi.Models.Entities;

namespace Qalam.MessagingApi.Services.Interfaces;

public interface IMessageQueueService
{
    Task QueueEmailAsync(EmailMessage emailMessage);
    Task QueueSmsAsync(SmsMessage smsMessage);
    Task QueuePushNotificationAsync(PushNotificationMessage pushMessage);
    Task QueueTeacherDocUploadAsync(TeacherDocUploadMessage message);
    Task QueueProfilePicUploadAsync(ProfilePicUploadMessage message);
}
