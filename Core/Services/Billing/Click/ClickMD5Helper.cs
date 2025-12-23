using System.Security.Cryptography;
using System.Text;

namespace Core.Services.Billing.Click;

public static class ClickMd5Helper
{
    private static string GetMd5Hash(string input)
    {
        var md5Hasher = MD5.Create();
        var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
        var sBuilder = new StringBuilder();

        foreach (var c in data)
        {
            sBuilder.Append(c.ToString("x2"));
        }

        return sBuilder.ToString();
    }

    public static bool VerifyMd5Hash(string input, string hash)
    {
        var hashOfInput = GetMd5Hash(input);
        var comparer = StringComparer.OrdinalIgnoreCase;

        return 0 == comparer.Compare(hashOfInput, hash);
    }
}