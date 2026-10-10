using System.Net;
using Hangfire.Dashboard;

namespace WebCore.Filters.Hangfire;

public class Authorization(IWebHostEnvironment environment) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        if (!environment.IsDevelopment() 
            && context.GetHttpContext().Request.Headers.TryGetValue("X-Forwarded-For", out var ip) && IPAddress.TryParse(ip.FirstOrDefault(), out var clientIp))
        {
            clientIp = clientIp.IsIPv4MappedToIPv6 ? clientIp.MapToIPv4() : clientIp;
            return IsTailscaleIp(clientIp);
        }

        return environment.IsDevelopment();
    }
    
    private static bool IsTailscaleIp(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        
        // 100.64.0.0/10 diapazoni:
        // 1-bayt = 100
        // 2-bayt 64 dan 127 gacha bo'ladi: (bytes[1] & 0xC0) == 64
        if (bytes.Length == 4)
        {
            return bytes[0] == 100 && (bytes[1] & 0xC0) == 64;
        }

        return false;
    }
}