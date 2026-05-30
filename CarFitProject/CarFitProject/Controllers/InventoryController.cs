using System.Security.Claims;
using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Controllers
{
    public class InventoryController : Controller
    {
        private const int PageSize = 12;

        private readonly CarFitDbContext _context;
        private readonly IInspectionScoringService _scoring;
        private readonly ISavedCarsService _savedCars;
        private readonly ISubscriptionService _subscriptions;

        public InventoryController(
            CarFitDbContext context,
            IInspectionScoringService scoring,
            ISavedCarsService savedCars,
            ISubscriptionService subscriptions)
        {
            _context = context;
            _scoring = scoring;
            _savedCars = savedCars;
            _subscriptions = subscriptions;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: /Inventory/Search?make=Toyota&priceTo=15000&page=2
        [HttpGet]
        public async Task<IActionResult> Search(ListingSearchViewModel filters)
        {
            var query = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Where(l => l.Status == "Active");

            if (!string.IsNullOrWhiteSpace(filters.Make))
                query = query.Where(l => l.Car!.Make.Contains(filters.Make));
            if (!string.IsNullOrWhiteSpace(filters.Model))
                query = query.Where(l => l.Car!.Model.Contains(filters.Model));
            if (filters.YearFrom.HasValue)
                query = query.Where(l => l.Car!.Year >= filters.YearFrom.Value);
            if (filters.YearTo.HasValue)
                query = query.Where(l => l.Car!.Year <= filters.YearTo.Value);
            if (filters.PriceFrom.HasValue)
                query = query.Where(l => l.ListingPrice >= filters.PriceFrom.Value);
            if (filters.PriceTo.HasValue)
                query = query.Where(l => l.ListingPrice <= filters.PriceTo.Value);
            if (!string.IsNullOrWhiteSpace(filters.Type))
                query = query.Where(l => l.Car!.Type == filters.Type);
            if (!string.IsNullOrWhiteSpace(filters.Transmission))
                query = query.Where(l => l.Car!.Transmission == filters.Transmission);

            filters.Results = await PaginatedList<CarListing>.CreateAsync(
                query.OrderByDescending(l => l.Id), filters.Page, PageSize);

            ViewBag.SavedCarIds = User.IsInRole("Buyer")
                ? await _savedCars.GetSavedCarIdsAsync(CurrentUserId!)
                : new HashSet<int>();
            return View(filters);
        }

        // GET: /Inventory/Detail/42
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var listing = await _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == id && l.Status == "Active");

            if (listing == null) return NotFound();

            if (listing.Car?.InspectionReport != null)
            {
                ViewBag.InspectionSignals = _scoring.Compute(listing.Car.InspectionReport);
            }

            ViewBag.IsBuyer = User.IsInRole("Buyer");
            if (ViewBag.IsBuyer)
            {
                ViewBag.IsSaved = await _context.SavedResults
                    .AnyAsync(s => s.UserId == CurrentUserId && s.CarId == listing.CarId);
                ViewBag.IsPremium = await _subscriptions.IsPremiumAsync(CurrentUserId);
            }

            var sellerCity = listing.Seller?.City;
            ViewBag.Mechanics = await _context.Mechanics
                .AsNoTracking()
                .OrderBy(m => m.City).ThenBy(m => m.Name)
                .ToListAsync();
            ViewBag.SellerCity = sellerCity;
            return View(listing);
        }

        // GET: /Inventory/GetTermExplanation?term=...
        [HttpGet]
        public async Task<IActionResult> GetTermExplanation(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return BadRequest();

            var match = await _context.InspectionTermsGlossaries
                .FirstOrDefaultAsync(g => g.Term.Trim().ToLower() == term.Trim().ToLower());

            if (match == null) return NotFound();

            return Json(new
            {
                severity = match.SeverityLevel,
                ar = match.ExplanationAr,
                en = match.ExplanationEn
            });
        }
    }
}
