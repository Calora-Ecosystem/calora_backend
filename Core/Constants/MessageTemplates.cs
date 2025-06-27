namespace Core.Constants;

public static class MessageTemplates
{
     public const string OtpSign = "Zingo ilovasiga kirish uchun bir martalik kod: {0}";
     
     public static string MakeMessage(string template, params object?[] args) => string.Format(template, args);
}