using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CarFitProject.Controllers
{
    [Authorize(Roles = "Buyer")]
    public class AdvisorController : Controller
    {
        private readonly CarFitDbContext _context;

        public AdvisorController(CarFitDbContext context)
        {
            _context = context;
        }

        // GET: /Advisor/Match
        public async Task<IActionResult> Match()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive == true);

            if (userProfile == null)
            {
                TempData["InfoMessage"] = "Please complete a lifestyle preference profile first to see your custom vehicle fits!";
                return RedirectToAction("Index", "Home");
            }

            // Fetch available listings through the database view 
            var availableCars = await _context.VwAvailableCarDetails.ToListAsync();

            var recommendationMatches = new List<RecommendedCarViewModel>();

            foreach (var car in availableCars)
            {
                int matchScore = 0;
                int totalCriteria = 0;

                // 1. Budget Compatibility Check (Handles nullable view variables safely)
                if (car.ListingPrice >= userProfile.BudgetMin && car.ListingPrice <= userProfile.BudgetMax)
                {
                    matchScore += 40;
                }
                totalCriteria += 40;

                // 2. Transmission Preference Check
                if (!string.IsNullOrEmpty(car.Transmission) &&
                    car.Transmission.Equals(userProfile.TransmissionPref, StringComparison.OrdinalIgnoreCase))
                {
                    matchScore += 30;
                }
                totalCriteria += 30;

                // 3. Family/Kids Size Capacity Check
                if (userProfile.HasKids == true && userProfile.KidsCount > 2)
                {
                    if (car.Seats >= 7 || (car.BodyType != null && car.BodyType.Contains("SUV", StringComparison.OrdinalIgnoreCase)))
                        matchScore += 30;
                }
                else
                {
                    matchScore += 30;
                }
                totalCriteria += 30;

                // Calculate total final match percentage
                int finalPercentageScore = (int)Math.Round((double)matchScore / totalCriteria * 100);

                // Only recommend vehicles that maintain a decent compatibility threshold
                if (finalPercentageScore >= 50)
                {
                    recommendationMatches.Add(new RecommendedCarViewModel
                    {
                        // FIX: Removed the invalid '??' operators for non-nullable primitive properties
                        CarId = car.CarId , // Keeps ?? ONLY if CarId is a nullable int? inside the model layout
                        Make = car.Make ?? "Unknown",
                        Model = car.Model ?? "Model",
                        Year = car.Year, // Removed ?? because Year is an 'int'
                        ListingPrice = car.ListingPrice ?? 0m, // Keeps ?? if ListingPrice is decimal?
                        City = car.City ?? "Jordan",
                        BodyCondition = car.BodyCondition ?? "Not Checked",
                        DescriptionScore = car.DescriptionScore ?? "N/A",
                        TrustScore = car.TrustScore, // Removed ?? because TrustScore is a 'decimal'
                        DynamicMatchScore = finalPercentageScore
                    });
                }
            }

            // Order matches so the highest compatibility profiles appear first
            var rankedResults = recommendationMatches.OrderByDescending(r => r.DynamicMatchScore).ToList();

            ViewBag.UserProfileName = userProfile.ProfileName;
            return View(rankedResults);
        }
    }
}