using Core.Enums;

namespace Core.Services.Notification.Contracts;

public record GetReminderMessageDto(long Id, EnumMomentType Type, EnumMenu? Menu, string Title, string? Description, TimeOnly? Time, bool IsActive);
public record GetReminderMessageShortDto(long Id, EnumMomentType Type, EnumMenu? Menu, string Title, TimeOnly? Time, bool IsActive);
