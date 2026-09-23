namespace Core.Constants;

public static class MessageTemplates
{
     public const string OtpSign =
          "{0} - Calora ilovasiga royxatdan otish uchun tasdiqlash uchun kod. Kodni hech kimga bermang.";
     
     public static string MakeMessage(string template, params object?[] args) => string.Format(template, args);
}