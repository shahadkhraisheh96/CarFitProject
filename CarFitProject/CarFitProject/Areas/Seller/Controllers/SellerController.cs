using CarFitProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarFitProject.Areas.Seller.Controllers
{
    [Authorize(Roles = "Seller,Dealer")] // Restricts to secondary market managers
    public class SellerController : Controller
    {
        private readonly CarFitDbContext _context;

        public SellerController(CarFitDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // FR-3.3: Fetch stock belonging explicitly to this seller
            var listings = await _context.CarListings
                .Include(l => l.Car)
                .ThenInclude(c => c.InspectionReport)
                .Where(l => l.SellerId.ToString() == userId)
                .ToListAsync();

            return View(listings);
        }
    }
}
