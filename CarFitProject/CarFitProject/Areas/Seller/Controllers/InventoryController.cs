using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CarFitProject.Models;

namespace CarFitProject.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Dealer")]
    public class InventoryController : Controller
    {
        private readonly CarFitDbContext _context;

        public InventoryController(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var sellerId = await _context.Sellers
                .Where(s => s.IdentityUserId == userId)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();

            if (sellerId == null)
            {
                return View(new List<CarListing>());
            }

            var listings = await _context.CarListings
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Where(l => l.SellerId == sellerId)
                .ToListAsync();

            return View(listings);
        }

        [HttpGet]
        public IActionResult AddCar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCar(Car carModel, decimal listingPrice, string paymentMethodAllowed)
        {
            if (carModel == null) return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (seller == null)
            {
                var displayName = User.Identity?.Name;
                seller = new Models.Seller
                {
                    IdentityUserId = userId,
                    Name = string.IsNullOrWhiteSpace(displayName) ? "Dealer" : displayName,
                    Email = User.FindFirstValue(ClaimTypes.Email),
                    IsApproved = false
                };
                _context.Sellers.Add(seller);
                await _context.SaveChangesAsync();
            }

            _context.Cars.Add(carModel);
            await _context.SaveChangesAsync();

            var listing = new CarListing
            {
                CarId = carModel.Id,
                SellerId = seller.Id,
                ListingPrice = listingPrice,
                PaymentMethodAllowed = paymentMethodAllowed,
                Available = true,
                InstallmentOption = !string.IsNullOrEmpty(paymentMethodAllowed)
                    && paymentMethodAllowed.Contains("Installment", StringComparison.OrdinalIgnoreCase)
            };

            _context.CarListings.Add(listing);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
