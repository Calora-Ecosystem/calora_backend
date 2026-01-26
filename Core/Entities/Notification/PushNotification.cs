using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities.Notification;

public class PushNotification : Notification
{
    public string? Image { get; set; } = null!;
    [Column(TypeName = "jsonb")] public Dictionary<string, string>? Meta { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? Scheduled { get; set; }
    public int FailureCount { get; set; }
    [MaxLength(500)]
    public string? FailureMessage { get; set; }
    public int SuccessCount { get; set; }
}