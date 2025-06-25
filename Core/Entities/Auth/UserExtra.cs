using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Refs;
using Core.Enums;

namespace Core.Entities.Auth;

public class UserExtra : ModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public double Weight { get; set; }
    public double Height { get; set; }
    public double Bmi { get; set; }
    public EnumGender Gender { get; set; }
    public DateTime BirthDate { get; set; }

    public User User { get; set; } = null!;
    public List<Purpose> Purposes { get; set; } = null!; //many2many
}