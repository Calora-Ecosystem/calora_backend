using System.ComponentModel.DataAnnotations;

namespace Core.Attributes;

public class LocalPhoneAttribute : RegularExpressionAttribute
{
    public LocalPhoneAttribute()
        : base("^\\+998([-| ])?(\\d{2})([-| ])?(\\d{3})([-| ])?(\\d{2})([-| ])?(\\d{2})$")
    {
        this.ErrorMessage = "Wrong Phone number format";
    }
}