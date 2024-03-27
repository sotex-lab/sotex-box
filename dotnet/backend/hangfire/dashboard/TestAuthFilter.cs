using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace backend.Hangfire.Dashboard;

public class TestAuthFilter : IDashboardAsyncAuthorizationFilter
{
    public Task<bool> AuthorizeAsync([NotNull] DashboardContext context)
    {
        return Task.FromResult(true);
    }
}
