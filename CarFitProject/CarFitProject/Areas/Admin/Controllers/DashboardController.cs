using CarFitProject.Models;
using Microsoft.AspNetCore.Authorization;
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

        public DashboardController(CarFitDbContext context)
        {
            _context = context;
        }

        // FR-7.5: Central Live Control Engine Base
        public async Task<IActionResult> Index()
        {
            // 1. Gather Metric KPI blocks
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalListings = await _context.CarListings.CountAsync(c => c.Available == true);
            ViewBag.TotalGlossaryTerms = await _context.InspectionTermsGlossaries.CountAsync();

            // 2. Prepare Analytics JSON payloads for Chart.js
            // Replace these static arrays with real LINQ queries if you need dynamic time-series data
            var chartLabels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            var inventoryGrowthData = new[] { 10, 25, 40, 75, 110, ViewBag.TotalListings };

            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartData = JsonSerializer.Serialize(inventoryGrowthData);

            // 3. Fetch latest activity records
            var recentListings = await _context.CarListings
                .Include(l => l.Car)
                .OrderByDescending(l => l.Id)
                .Take(5)
                .ToListAsync();

            return View(recentListings);
        }
    }
}