using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CarFitProject.Models;
using CarFitProject.Services;

namespace CarFitProject.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class DashboardController : Controller
    {
        private readonly CarFitDbContext _context;
        private readonly IRecommendationService _recommendations;

        public DashboardController(CarFitDbContext context, IRecommendationService recommendations)
        {
            _context = context;
            _recommendations = recommendations;
        }

        public async Task<IActionResult> Index(int? activeProfileId)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var profiles = await _context.UserProfiles
                .Where(p => p.UserId == userId && p.IsActive == true)
                .ToListAsync();

            if (!profiles.Any())
            {
                return RedirectToAction("CreateProfile");
            }

            var selectedProfile = activeProfileId.HasValue
                ? profiles.FirstOrDefault(p => p.ProfileId == activeProfileId.Value)
                : profiles.First();

            selectedProfile ??= profiles.First();

            ViewBag.AllUserProfiles = profiles;
            ViewBag.CurrentProfileId = selectedProfile.ProfileId;
            ViewBag.CurrentProfileName = selectedProfile.ProfileName;

            var matches = await _recommendations.GetMatchesAsync(selectedProfile);
            return View(matches);
        }

        [HttpGet]
        public IActionResult CreateProfile()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(UserProfile model)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Challenge();

            model.UserId = userId;
            model.IsActive = true;

            if (string.IsNullOrEmpty(model.ProfileName)) model.ProfileName = "My Lifestyle Fit";
            if (string.IsNullOrEmpty(model.TransmissionPref)) model.TransmissionPref = "Automatic";
            if (string.IsNullOrEmpty(model.SizePref)) model.SizePref = "Sedan";
            if (string.IsNullOrEmpty(model.Purpose)) model.Purpose = "Commuting";

            _context.UserProfiles.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { activeProfileId = model.ProfileId });
        }
    }
}
