using System.ComponentModel.DataAnnotations;

namespace Core.Services.Auth.Contracts;

public class DeviceDto
{
    public string Key { get; set; } = null!;
    [MaxLength(100)] public string Name { get; set; } = null!;
    public string? FcmToken { get; set; }
}