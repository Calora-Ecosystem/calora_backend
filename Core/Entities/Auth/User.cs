using System.ComponentModel.DataAnnotations;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(Email))]
[Index(nameof(Name))]
public class User : ModelBase<long>
{
    [MaxLength(100)] public string Email { get; set; } = null!;
    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(64)] public string Password { get; set; } = null!;
    public string? Photo { get; set; }
}