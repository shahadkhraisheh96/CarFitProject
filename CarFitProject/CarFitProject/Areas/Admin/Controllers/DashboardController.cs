using CarFitProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CarFitProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly CarFitDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(CarFitDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // FR-7.5: Central Live Control Engine Base
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalListings = await _context.CarListings.CountAsync(c => c.Available == true);
            ViewBag.TotalGlossaryTerms = await _context.InspectionTermsGlossaries.CountAsync();

            var chartLabels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            var inventoryGrowthData = new[] { 10, 25, 40, 75, 110, ViewBag.TotalListings };

            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartData = JsonSerializer.Serialize(inventoryGrowthData);

            var recentListings = await _context.CarListings
                .Include(l => l.Car)
                .OrderByDescending(l => l.Id)
                .Take(5)
                .ToListAsync();

            return View(recentListings);
        }
    }
}
