using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities.Notification;

public class PushNotification : Notification
{
    public string? Image { get; set; } = null!;
    [Column(TypeName = "jsonb")] public Dictionary<string, string>? Meta { get; set; }
    public DateTime? EnqueuedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? Scheduled { get; set; }
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }

    /// <summary>
    /// When set, this push is skipped at send time if the user has already logged a
    /// <see cref="DailyMenu"/> entry for this menu today ("only remind if not logged").
    /// </summary>
    public EnumMenu? MealGateMenu { get; set; }
}