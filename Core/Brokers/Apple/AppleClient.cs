using Microsoft.IdentityModel.Tokens;

namespace Core.Brokers.Apple;

public class AppleClient(IHttpClientFactory httpClientFactory)
{
    public async Task<JsonWebKeySet> FetchAppleJwkSet()
    {
        var json = await httpClientFactory.CreateClient("apple-jwks")
            .GetStringAsync("");
        return JsonWebKeySet.Create(json);
    }
}