using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Course;

[Index(nameof(Order))]
public abstract class BaseItem : ModelBase<long>
{
    [MaxLength(100)] public MultiLanguageField Title { get; set; } = null!;

    [MaxLength(500)] public MultiLanguageField Description { get; set; } = null!;
    [Column(TypeName = "jsonb")] public Asset[] Assets { get; set; } = null!;
    [Column(TypeName = "decimal(10,3)")] public decimal Order { get; set; } = 1;
}