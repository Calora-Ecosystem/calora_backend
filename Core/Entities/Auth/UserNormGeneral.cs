using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Enums;

namespace Core.Entities.Auth;

public class UserNormGeneral : ModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }

    public User User { get; set; } = null!;
}