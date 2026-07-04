using Ardayasa.Domain;
using Hangfire.Dashboard;

namespace Ardayasa.Api.Infrastructure;

/// <summary>
/// Restricts /hangfire to admins. In Development, local requests are also allowed
/// (the dashboard is browser-navigated, so no Bearer token is attached).
/// </summary>
public class HangfireDashboardAuthorizationFilter(bool isDevelopment) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole(Roles.Admin))
        {
            return true;
        }

        return isDevelopment && context.IsReadOnly is false && IsLocal(httpContext);
    }

    private static bool IsLocal(Microsoft.AspNetCore.Http.HttpContext ctx)
    {
        var remote = ctx.Connection.RemoteIpAddress;
        return remote is not null
            && (System.Net.IPAddress.IsLoopback(remote) || remote.Equals(ctx.Connection.LocalIpAddress));
    }
}
