namespace Core.Helpers;

public static class FormatHelper
{
    public static string MakeValidPhone(string raw)
    {
        var validPhone = raw.Replace("+", "");

        if (validPhone.StartsWith("998"))
            validPhone = validPhone.Replace("998", "");

        return validPhone;
    }
}