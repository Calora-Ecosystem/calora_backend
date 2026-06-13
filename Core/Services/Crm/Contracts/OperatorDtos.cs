using System.ComponentModel.DataAnnotations;

namespace Core.Services.Crm.Contracts;

public record CreateOperatorDto
{
    [Required, MaxLength(300)] public string Name { get; set; } = null!;
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = null!;
    [MaxLength(50)] public string? Phone { get; set; }
}

public record GetMeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public IEnumerable<string> Roles { get; set; } = [];
}
