using BRB.Core.Common.Models;

namespace Core.Services.Course.Common;

public class BaseCreateOrUpdateDto
{
    public long? Id { get; set; }
    public MultiLanguageField Title { get; set; } = null!;
    public MultiLanguageField Description { get; set; } = null!;
}