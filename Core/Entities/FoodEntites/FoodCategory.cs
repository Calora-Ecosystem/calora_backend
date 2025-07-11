using BRB.Core.Common.Models;
using BRB.Core.Common.Models.Base;

namespace Core.Entities.FoodEntites;

public class FoodCategory : ModelBase<long>
{
    public MultiLanguageField Name { get; set; } = null!;
    public string CoverUrl { get; set; } = null!;
}