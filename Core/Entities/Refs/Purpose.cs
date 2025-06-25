using System.ComponentModel.DataAnnotations;
using BRB.Core.Common.Models.Base;

namespace Core.Entities.Refs;

public class Purpose : ReferenceModelBase<long>
{
    [MaxLength(500)]
    public string Description { get; set; } = null!;
}