using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Brokers.EmailBroker;

namespace Core.Services.Notification;

[Injectable]
public partial class NotificationService(EmailClient emailClient, AppDbContext dbContext)
{
}