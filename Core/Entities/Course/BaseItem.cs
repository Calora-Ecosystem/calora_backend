using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models;
using BRB.Core.Common.Models.Base;

namespace Core.Entities.Course;

public abstract class BaseItem : ModelBase<long>
{
    [MaxLength(100)] public MultiLanguageField Title { get; set; } = null!;

    [MaxLength(500)] public MultiLanguageField Description { get; set; } = null!;
    [Column(TypeName = "jsonb")] public string[] Assets { get; set; } = null!;
}