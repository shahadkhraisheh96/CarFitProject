using CarFitProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Controllers
{
    public class InventoryController : Controller
    {
        private readonly CarFitDbContext _context;

        public InventoryController(CarFitDbContext context)
        {
            _context = context;
        }

        // GET: /Inventory/Search
        [HttpGet]
        public async Task<IActionResult> Search()
        {
            // Pull only available vehicles to the customer catalog
            var listings = await _context.CarListings
                .Include(l => l.Car)
                .Where(l => l.Status == "Active")
                .OrderByDescending(l => l.Id)
                .ToListAsync();

            return View(listings);
        }
        // GET: /Inventory/GetTermExplanation?term=جيد
        [HttpGet]
        public async Task<IActionResult> GetTermExplanation(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest();

            // Query your SQL Server glossary index context array pool
            var dictionaryMatch = await _context.InspectionTermsGlossaries
                .FirstOrDefaultAsync(g => g.Term.Trim().ToLower() == term.Trim().ToLower());

            if (dictionaryMatch == null)
                return NotFound();

            // Return structured bilingual metadata properties payload
            return Json(new
            {
                severity = dictionaryMatch.SeverityLevel, // e.g., Low, Medium, High, Critical
                ar = dictionaryMatch.ExplanationAr,
                en = dictionaryMatch.ExplanationEn
            });
        }
    }
}
