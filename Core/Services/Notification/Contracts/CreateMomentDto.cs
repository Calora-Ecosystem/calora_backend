using BRB.Core.Common.Models;

namespace Core.Services.Notification.Contracts;

public class CreateMomentDto
{
    public TimeOnly Time { get; set; }
    public MultiLanguageField Name { get; set; } = default!;
}