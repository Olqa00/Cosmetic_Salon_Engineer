using Engineer_MVC.Data;
using Engineer_MVC.Data.Interfaces;
using Engineer_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Engineer_MVC.Controllers
{
    public class HomeController : CustomBaseController
    {
        private readonly ILogger<HomeController> _logger;
		private readonly IEmailSender _emailSender;

		public HomeController(ILogger<HomeController> logger,
            IEmailSender emailSender)
        {
            _logger = logger;
			_emailSender= emailSender;

		}

        public IActionResult Index()
        {

			return View();
        }
        public IActionResult AboutUs()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}