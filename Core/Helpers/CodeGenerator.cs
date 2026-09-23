using System.Security.Cryptography;

namespace Core.Helpers;

/// <summary>
/// Odam o'qiy oladigan tasodifiy kodlar (taklif kodi, guruh kodi, vaucher).
/// Chalkash belgilar (0/O, 1/I/L) ishlatilmaydi.
/// </summary>
public static class CodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(string prefix, int length) =>
        prefix + RandomNumberGenerator.GetString(Alphabet, length);
}
