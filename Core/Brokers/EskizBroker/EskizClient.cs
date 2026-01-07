using System.Net.Http.Json;
using System.Text.Json;
using BRB.Core.Common.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Serilog;

namespace Core.Brokers.EskizBroker;

public class EskizClient(IOptions<EskizConfig> configOptions, IMemoryCache memoryCache)
{
    private HttpClient GetClient()
    {
        return new HttpClient() { BaseAddress = new Uri(configOptions.Value.BaseUrl) };
    }

    private async Task<string> GetAccToken()
    {
        if (memoryCache.TryGetValue<string>("eskiz_acc_token", out var value) && !value.IsNullOrEmpty())
            return value!;

        var client = GetClient();
        var content = new MultipartFormDataContent();

        content.Add(new StringContent(configOptions.Value.Login), "email");
        content.Add(new StringContent(configOptions.Value.Password), "password");

        var response = await client.PostAsync("auth/login", content);

        if (!response.IsSuccessStatusCode)
        {
#if DEBUG
            Log.Error(await response.Content.ReadAsStringAsync());
#endif
            throw new Exception("Sms service error");
        }

        var respBody = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (respBody.TryGetProperty("data", out var data) && data.TryGetProperty("token", out var token) &&
            token.GetString() is { } tokenValue && !tokenValue.IsNullOrEmpty())
        {
            memoryCache.Set("eskiz_acc_token", tokenValue, TimeSpan.FromDays(28));
            return tokenValue;
        }

        throw new Exception("Sms service error");
    }

    private async Task<HttpClient> GetClientWithAuth()
    {
        var client = GetClient();
        var token = await GetAccToken();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    public async Task SendMessage(string phone, string message)
    {
        var client = await GetClientWithAuth();

        var content = new MultipartFormDataContent();

        content.Add(new StringContent(phone), "mobile_phone");
        content.Add(new StringContent(message), "message");
        content.Add(new StringContent("4546"), "from");

        var response = await client.PostAsync("message/sms/send",
            content);

        if (response.IsSuccessStatusCode)
            throw new Exception("Unable to send sms");
    }
}