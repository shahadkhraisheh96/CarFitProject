using CarFitProject.Areas.Admin.Models;
using CarFitProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CarFitProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly CarFitDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(CarFitDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // FR-7.5: Analytics & Overview Dashboard Base Redirect to Dashboard Area
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        // FR-7.1: User Management Console Identity Registry List
        public async Task<IActionResult> UsersList()
        {
            var userAccounts = await _userManager.Users.OrderByDescending(u => u.Id).ToListAsync();
            return View(userAccounts);
        }

        // FR-7.4: Inspection Glossary Management Data View
        public async Task<IActionResult> GlossaryManager()
        {
            var terms = await _context.InspectionTermsGlossaries.ToListAsync();
            return View(terms);
        }

        // FR-7.4: Add or Modify Arabic/English Dictionary Jargon
        [HttpPost]
        [ValidateAntiForgeryToken] // CSRF Protection requirement
        public async Task<IActionResult> UpdateGlossaryTerm(InspectionTermsGlossary model)
        {
            if (!ModelState.IsValid) return RedirectToAction("GlossaryManager");

            var trackingEntity = await _context.InspectionTermsGlossaries
                .FirstOrDefaultAsync(g => g.Term == model.Term);

            if (trackingEntity == null)
            {
                _context.InspectionTermsGlossaries.Add(model);
            }
            else
            {
                trackingEntity.SeverityLevel = model.SeverityLevel;
                trackingEntity.ExplanationAr = model.ExplanationAr;
                trackingEntity.ExplanationEn = model.ExplanationEn;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("GlossaryManager");
        }

        // GET: Areas/Admin/Views/Admin/BulkLoader.cshtml Staging Interface
        [HttpGet]
        public IActionResult BulkLoader()
        {
            return View(new BulkImportViewModel());
        }

        // POST: Parse CSV Buffer Stream and Save Entities to SQL Server Database (FR-7.2)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkLoader(IFormFile csvFile)
        {
            var viewModel = new BulkImportViewModel();

            if (csvFile == null || csvFile.Length == 0)
            {
                viewModel.ErrorMessages.Add("Missing structural file handle. Please upload a valid CSV file stream.");
                return View(viewModel);
            }

            try
            {
                var listingsToInsert = new List<CarListing>();

                using (var reader = new StreamReader(csvFile.OpenReadStream()))
                {
                    int lineIndex = 0;
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        lineIndex++;

                        // Bypass the spreadsheet header matrix configuration array line
                        if (lineIndex == 1) continue;

                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Parse out line text records by target commas split matrix
                        var values = line.Split(',');

                        // Validation: Expected Format: Make,Model,Year,Trim,Price
                        if (values.Length < 5)
                        {
                            viewModel.ErrorMessages.Add($"Line {lineIndex}: Missing parameters. Row contains less than 5 required columns.");
                            continue;
                        }

                        // Extract parameters securely out of data index tokens
                        string make = values[0]?.Trim();
                        string model = values[1]?.Trim();

                        if (!int.TryParse(values[2]?.Trim(), out int year))
                        {
                            viewModel.ErrorMessages.Add($"Line {lineIndex}: Invalid numerical configuration format for production 'Year'.");
                            continue;
                        }

                        string trim = values[3]?.Trim();

                        if (!decimal.TryParse(values[4]?.Trim(), out decimal price))
                        {
                            viewModel.ErrorMessages.Add($"Line {lineIndex}: Invalid decimal financial formatting for 'ListingPrice'.");
                            continue;
                        }

                        // Assemble structural model relationship nodes
                        var newListing = new CarListing
                        {
                            Available = true,
                            ListingPrice = price,
                            // Building inner relational Car database entity parameters
                            Car = new Car
                            {
                                Make = make,
                                Model = model,
                                Year = year,
                                Trim = string.IsNullOrWhiteSpace(trim) ? "Base" : trim
                            }
                        };

                        listingsToInsert.Add(newListing);
                        viewModel.InsertedCount++;
                    }
                }

                // Database Transaction Criteria: Only save if the data sheets are 100% clean
                if (listingsToInsert.Count > 0 && viewModel.ErrorMessages.Count == 0)
                {
                    await _context.CarListings.AddRangeAsync(listingsToInsert);
                    await _context.SaveChangesAsync();
                    viewModel.IsSuccess = true;
                }
                else if (viewModel.ErrorMessages.Count > 0)
                {
                    // Roll back insertion numbers if error anomalies are detected anywhere
                    viewModel.IsSuccess = false;
                    viewModel.InsertedCount = 0;
                }
            }
            catch (Exception ex)
            {
                viewModel.IsSuccess = false;
                viewModel.ErrorMessages.Add($"Critical execution breakout error exception: {ex.Message}");
            }

            return View(viewModel);
        }
    }
}