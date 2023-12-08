using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Engineer_MVC.Controllers
{
    [Route("languages")]
    public class LanguageController : CustomBaseController
    {
        [Route("change")]
        public IActionResult Change(string culture)
        {

            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddMonths(1) }
                );
            return RedirectToAction("index", "home");
        }
    }
}
