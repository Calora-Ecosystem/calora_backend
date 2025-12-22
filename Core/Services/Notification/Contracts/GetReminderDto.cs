using Core.Enums;

namespace Core.Services.Notification.Contracts;

public record GetReminderDto(long Id, EnumMomentType Type, EnumMenu? Menu, TimeSpan Time);