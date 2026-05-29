using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CarFitProject.Models;
using CarFitProject.ViewModel;

namespace CarFitProject.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class DashboardController : Controller
    {
        private readonly CarFitDbContext _context;

        public DashboardController(CarFitDbContext context)
        {
            _context = context;
        }

        // Action 1: Load Dashboard Recommendations Portfolio
        public async Task<IActionResult> Index(int? activeProfileId)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Fetches all matching profiles saved under this active login context
            var profiles = await _context.UserProfiles
                .Where(p => p.UserId == userId && p.IsActive == true)
                .ToListAsync();

            // If a new user doesn't have a persona built yet, send them to the setup wizard
            if (!profiles.Any())
            {
                return RedirectToAction("CreateProfile");
            }

            var selectedProfile = activeProfileId.HasValue
                ? profiles.FirstOrDefault(p => p.ProfileId == activeProfileId.Value)
                : profiles.First();

            if (selectedProfile == null) selectedProfile = profiles.First();

            ViewBag.AllUserProfiles = profiles;
            ViewBag.CurrentProfileId = selectedProfile.ProfileId;
            ViewBag.CurrentProfileName = selectedProfile.ProfileName;

            // 1. Prepare an empty tracking container collection matching your view requirements
            var matches = new List<RecommendedCarViewModel>();

            // 2. Open an isolated database command channel via ADO.NET to bypass the OPENROWSET block securely
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "GetCarMatchesForUser";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                // Bind the incoming active profile filter parameter safely
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@ProfileId";
                parameter.Value = selectedProfile.ProfileId;
                command.Parameters.Add(parameter);

                if (command.Connection.State != System.Data.ConnectionState.Open)
                {
                    await command.Connection.OpenAsync();
                }

                // 3. Read the output lines and map snake_case SQL outputs straight into PascalCase properties
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        matches.Add(new RecommendedCarViewModel
                        {
                            CarId = reader.GetInt32(reader.GetOrdinal("car_id")),
                            Make = reader.GetString(reader.GetOrdinal("make")),
                            Model = reader.GetString(reader.GetOrdinal("model")),
                            Year = reader.GetInt32(reader.GetOrdinal("year")),
                            ListingPrice = reader.GetDecimal(reader.GetOrdinal("listing_price")),
                            City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                            BodyCondition = reader.IsDBNull(reader.GetOrdinal("body_condition")) ? null : reader.GetString(reader.GetOrdinal("body_condition")),
                            DescriptionScore = reader.IsDBNull(reader.GetOrdinal("description_score")) ? null : reader.GetString(reader.GetOrdinal("description_score")),
                            TrustScore = reader.GetDecimal(reader.GetOrdinal("trust_score")),
                            DynamicMatchScore = reader.GetInt32(reader.GetOrdinal("DynamicMatchScore"))
                        });
                    }
                }
            }

            return View(matches);
        }

        // Action 2: Display Questionnaire Form Page
        [HttpGet]
        public IActionResult CreateProfile()
        {
            return View();
        }

        // Action 3: Process Questionnaire Submission Block
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(UserProfile model)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Challenge();

            model.UserId = userId;
            model.IsActive = true;

            // Safety Guardrails: Replaces empty fields with default strings to prevent database table structural violations
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