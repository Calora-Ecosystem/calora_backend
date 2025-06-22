using System.Text.Json;
using System.Text.Json.Serialization;
using BRB.Core.Common.Models;

namespace WebCore.Converters;

public class MultiLanguageFieldConverter : JsonConverter<MultiLanguageField>
{
    private readonly IHttpContextAccessor _contextAccessor;

    public MultiLanguageFieldConverter(IHttpContextAccessor contextAccessor)
    {
        this._contextAccessor = contextAccessor;
    }

    public override MultiLanguageField? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var mlf = Activator.CreateInstance(typeToConvert);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var propName = reader.GetString();
            var prop = typeToConvert.GetProperties()
                .FirstOrDefault(x => x.Name.Equals(propName, StringComparison.InvariantCultureIgnoreCase));
            reader.Read();
            if (prop is not null)
                prop!.SetValue(mlf, reader.GetString());
        }

        return (MultiLanguageField)mlf!;
    }

    public override void Write(Utf8JsonWriter writer, MultiLanguageField value, JsonSerializerOptions options)
    {
        var langCode = this._contextAccessor.HttpContext!.Request.Headers.AcceptLanguage.FirstOrDefault();

        switch (langCode)
        {
            case "UZ":
                writer.WriteStringValue(value.Uz);
                break;
            case "RU":
                writer.WriteStringValue(value.Ru);
                break;
            case "CYRL":
                writer.WriteStringValue(value.Cyrl);
                break;
            case "ENG":
                writer.WriteStringValue(value.Eng);
                break;
            default:
                writer.WriteRawValue(value.ToString(), true);
                break;
        }
    }
}