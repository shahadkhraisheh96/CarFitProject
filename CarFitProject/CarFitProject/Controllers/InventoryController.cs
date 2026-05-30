using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Controllers
{
    public class InventoryController : Controller
    {
        private const int PageSize = 12;

        private readonly CarFitDbContext _context;

        public InventoryController(CarFitDbContext context)
        {
            _context = context;
        }

        // GET: /Inventory/Search?make=Toyota&priceTo=15000&page=2
        [HttpGet]
        public async Task<IActionResult> Search(ListingSearchViewModel filters)
        {
            var query = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
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
