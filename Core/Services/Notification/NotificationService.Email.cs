using Core.Services.Notification.Contracts;
using Core.Services.Notification.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Notification;

public partial class NotificationService
{
    public async Task SendMailAsync(EmailNotificationDto notification)
    {
        if (notification.Description is null)
            throw new NotificationDescriptionRequiredException();
        
        var user = await dbContext.Users
            .Select(x => new { x.Id, x.Email })
            .FirstOrDefaultAsync(x => x.Id == notification.UserId);

        await emailClient.SendMailAsync(user!.Email, notification.Description, notification.Title);
    }
    
    public async Task SendMailAsync(EmailNotificationWithoutUserDto notification)
    {
        if (notification.Description is null)
            throw new NotificationDescriptionRequiredException();
        
        await emailClient.SendMailAsync(notification.Email, notification.Description, notification.Title);
    }
    
    public async Task SendMailAsync(BatchEmailNotificationDto notification)
    {
        if (notification.Description is null)
            throw new NotificationDescriptionRequiredException();
        
        var tasks = (await dbContext.Users
                .Where(x => notification.UserIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Email }).ToListAsync())
            .Select(user => emailClient.SendMailAsync(user!.Email, notification.Description, notification.Title));

        await Task.WhenAll(tasks);
    }
}