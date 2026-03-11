using System.Globalization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Localization;

namespace Econyx.Dashboard;

public sealed class CultureCircuitHandler(IHttpContextAccessor httpContextAccessor) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return Task.CompletedTask;

        var feature = httpContext.Features.Get<IRequestCultureFeature>();
        if (feature is null) return Task.CompletedTask;

        var cultureInfo = feature.RequestCulture.UICulture;
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        return Task.CompletedTask;
    }
}
