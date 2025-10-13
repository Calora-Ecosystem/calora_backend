using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.FoodEntites;
using Core.Entities.Refs;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(Name))]
public class UserExtra : ModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public double Weight { get; set; }
    public double Height { get; set; }
    public double Bmi { get; set; }
    public EnumGender Gender { get; set; }
    public DateTime BirthDate { get; set; }

    public User User { get; set; } = null!;
    public EnumPurpose Purpose { get; set; }
    public string? Photo { get; set; }
    public EnumActivityLevel ActivityLevel { get; set; } = EnumActivityLevel.Medium;
    public EnumLanguage Language { get; set; } = EnumLanguage.Uzbek;
    [MaxLength(100)] public string Name { get; set; } = null!;
    public IQueryable<Food> FavouriteFoods { get; set; } = null!; //many2many
}