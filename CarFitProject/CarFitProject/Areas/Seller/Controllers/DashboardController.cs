using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarFitProject.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Dealer")]
    public class DashboardController : Controller
    {
       
        public IActionResult Index()
        {
            return View();
        }
    }
}
