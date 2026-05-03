namespace Core.Constants;

public static class MessageTemplates
{
     public const string OtpSign =
          "Calora ilovasiga royxatdan otish uchun tasdiqlash kodi: {0}. Kodni hech kimga bermang.";
     
     public static string MakeMessage(string template, params object?[] args) => string.Format(template, args);
}