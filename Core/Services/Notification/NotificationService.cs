using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Brokers.EmailBroker;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Notification;

[Injectable]
public partial class NotificationService(EmailClient emailClient, AppDbContext dbContext)
{
    public async Task<int> GetUnreadNotificationsCount(long userId)
    {
        return await dbContext.Notifications.CountAsync(x => x.UserId == userId && x.HasRead);
    }
}