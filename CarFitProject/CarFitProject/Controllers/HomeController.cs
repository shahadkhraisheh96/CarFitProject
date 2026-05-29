using CarFitProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CarFitProject.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // If user isn't logged in, show the public marketing page
            if (!User.Identity.IsAuthenticated)
            {
                return View();
            }

            // Role Traffic Management Routing Matrix
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else if (User.IsInRole("Seller"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Seller" });
            }
            else if (User.IsInRole("Buyer"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Buyer" });
            }
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
