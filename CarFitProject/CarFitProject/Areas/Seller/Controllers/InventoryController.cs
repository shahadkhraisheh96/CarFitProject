using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CarFitProject.Models;

namespace CarFitProject.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Seller")]
    public class InventoryController : Controller
    {
        private readonly CarFitDbContext _context;

        public InventoryController(CarFitDbContext context)
        {
            _context = context;
        }

        // Action 1: List all active inventory belonging to the dealership dealership
        public async Task<IActionResult> Index()
        {
            // Fetch listing details joined securely across your base Cars dataset
            var listings = await _context.CarListings
                .Include(l => l.Car)
                .ToListAsync();

            return View(listings);
        }

        // Action 2: Get View form to insert a brand new car listing
        [HttpGet]
        public IActionResult AddCar()
        {
            return View();
        }

        // Action 3: Process the form submission data and update SQL Server tables
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCar(Car carModel, decimal listingPrice, string paymentMethodAllowed)
        {
            if (carModel == null) return BadRequest();

            // 1. Commit the base physical car technical metrics specifications block
            _context.Cars.Add(carModel);
            await _context.SaveChangesAsync(); // Generates the Identity ID seed value

            // 2. Build the commercial marketplace listing row tied to that car ID
            var listing = new CarListing
            {
                CarId = carModel.Id,
                ListingPrice = listingPrice,
                PaymentMethodAllowed = paymentMethodAllowed,
                Available = true,
                InstallmentOption = paymentMethodAllowed.Contains("Installment", StringComparison.OrdinalIgnoreCase)
            };

            _context.CarListings.Add(listing);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}