using Core.Entities.Course.Enum;

namespace Core.Entities.Course;

public class Asset
{
    public EnumAssetType Type { get; set; } = EnumAssetType.Default;
    public string Url { get; set; } = null!;
}