using Core.Brokers.DbContext;
using Core.Brokers.EmailBroker;

namespace Core.Services.Notification;

public partial class NotificationService(EmailClient emailClient, AppDbContext dbContext)
{
}