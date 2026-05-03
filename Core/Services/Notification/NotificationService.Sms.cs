using Core.Services.Notification.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Services.Notification;

public partial class NotificationService
{
    public async Task SendSms(SmsNotificationDto dto)
    {
        await eskizClient.SendMessage(dto.Phone, dto.Description!);
        logger.LogInformation("SMS sent to {Phone} - {Title}", dto.Phone, dto.Title);
    }
}