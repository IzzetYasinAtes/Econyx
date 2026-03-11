using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Econyx.Dashboard.Controllers;

[Route("[controller]/[action]")]
public sealed class CultureController : Controller
{
    public IActionResult Set(string culture, string returnUrl)
    {
        HttpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        return LocalRedirect(returnUrl);
    }
}
