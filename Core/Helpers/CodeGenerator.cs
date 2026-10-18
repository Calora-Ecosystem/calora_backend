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

    /// <summary>
    /// User kiritgan kodni bazadagi ko'rinishga keltiradi: faqat harf va raqamlar, katta harfda.
    /// Chiziqcha va bo'shliqlar tushib qoladi — avval "CAL-XXXXX" ko'rinishida ulashilgan kodlar ham topiladi.
    /// </summary>
    public static string Normalize(string code) =>
        new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}
