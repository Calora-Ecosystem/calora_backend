using BRB.Core.Common.Models;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Refs;

[Index(nameof(Time))]
public class Moment : ReferenceModelBase<long>
{
    public MultiLanguageField Name { get; set; } = null!;
    public TimeOnly Time { get; set; }
}