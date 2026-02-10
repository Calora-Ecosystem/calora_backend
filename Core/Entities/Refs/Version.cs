using System.ComponentModel.DataAnnotations;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Refs;

[Index(nameof(Key), IsUnique = true)]
public class Version : AuditableModelBase<long>
{
    [MaxLength(20)] public string Key { get; set; } = null!;
    public bool IsActive { get; set; }
}