namespace Core.Services.Notification.Contracts;

public record SendPushResultDto
{
    public int Total { get; set; }
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
}