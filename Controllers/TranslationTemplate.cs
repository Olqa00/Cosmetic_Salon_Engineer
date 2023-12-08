using Engineer_MVC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace Engineer_MVC.Controllers
{
    public class TranslationTemplate
    {
        private readonly EngineerContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IStringLocalizer<SharedResource> _sharedResource;

        public TranslationTemplate(EngineerContext context, IWebHostEnvironment hostEnvironment,
            IStringLocalizer<SharedResource> sharedResource)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _sharedResource = sharedResource;
        }

        public IActionResult Translate(string template)
        {
            var regex = new Regex(@"@sharedResource\[""([^""]+)""\]");
            var matches = regex.Matches(template);

            foreach (Match match in matches)
            {
                var originalText = match.Value;
                var translationKey = match.Groups[1].Value;

                var translatedText = _sharedResource[translationKey];

                template = template.Replace(originalText, translatedText);
            }
            return new ContentResult
            {
                Content = template,
                ContentType = "text/html",
                StatusCode = 200
            };
        }
    }

}
