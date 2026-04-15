using Hangfire.Dashboard;

namespace WebCore.Filters.Hangfire;

public class Authorization(IWebHostEnvironment environment) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return !environment.IsProduction();
    }
}