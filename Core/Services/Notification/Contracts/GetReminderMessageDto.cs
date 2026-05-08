using Core.Enums;

namespace Core.Services.Notification.Contracts;

public record GetReminderMessageDto(long Id, EnumMomentType Type, EnumMenu? Menu, string Title, string? Description);
public record GetReminderMessageShortDto(long Id, EnumMomentType Type, EnumMenu? Menu, string Title);
