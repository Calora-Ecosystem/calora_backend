namespace Core.Services.Auth;

/// <summary>
/// VAQTINCHALIK (SMS balansi tugagan paytda test uchun): SMS ketmagan OTP kodni
/// Telegram botga yuboradi. SMS qayta ishlagach bu fayl va chaqiruvi o'chiriladi.
/// </summary>
public static class OtpTelegramNotifier
{
    private const string BotToken = "8081833014:AAF41kezEPCyvIe5bvyAes7EgHETI7sntoo";
    private const string ChatId = "1726806055";

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>Xato bo'lsa jim o'tadi — OTP so'rovi buzilmasligi kerak.</summary>
    public static async Task Send(string phone, string otp)
    {
        try
        {
            await Client.PostAsync($"https://api.telegram.org/bot{BotToken}/sendMessage",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["chat_id"] = ChatId,
                    ["text"] = $"📱 {phone}\n🔑 OTP: {otp}"
                }));
        }
        catch
        {
            // Telegram ishlamasa ham OTP server logida bor.
        }
    }
}
