using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Services.User.Contracts;

public record TeamMemberDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];

    /// <summary>Operator sifatida biriktirilgan, yakunlanmagan leadlar soni.</summary>
    public int ActiveLeads { get; set; }

    public DateTime CreatedAt { get; set; }
}

public record CreateTeamMemberDto
{
    [Required, MaxLength(300)] public string Name { get; set; } = null!;
    [Required, EmailAddress, MaxLength(100)] public string Email { get; set; } = null!;
    [MaxLength(50)] public string? Phone { get; set; }

    /// <summary>Kamida bitta jamoa roli: SuperAdmin, HeadOfSales yoki Operator.</summary>
    [Required, MinLength(1)] public EnumRole[] Roles { get; set; } = [];
}

public record UpdateTeamMemberDto
{
    [Required, MaxLength(300)] public string Name { get; set; } = null!;
    [MaxLength(50)] public string? Phone { get; set; }
    [Required, MinLength(1)] public EnumRole[] Roles { get; set; } = [];
}
