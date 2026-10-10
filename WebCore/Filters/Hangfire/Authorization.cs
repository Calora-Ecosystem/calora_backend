using System.Net;
using Hangfire.Dashboard;

namespace WebCore.Filters.Hangfire;

public class Authorization(IWebHostEnvironment environment) : IDashboardAuthorizationFilter
{
    private readonly IPNetwork TailScaleSubnet = IPNetwork.Parse("100.64.0.0/10");
    public bool Authorize(DashboardContext context)
    {
        if (!environment.IsDevelopment() &&
            IPAddress.TryParse(context.Request.RemoteIpAddress, out var clientIp))
        {
            clientIp = clientIp.IsIPv4MappedToIPv6 ? clientIp.MapToIPv4() : clientIp;
            return TailScaleSubnet.Contains(clientIp);
        }

        return environment.IsDevelopment();
    }
}