using CarFitProject.Areas.Admin.Models;
using CarFitProject.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        // POST: Parse CSV with CsvHelper and import per-row; valid rows commit, invalid rows are reported (FR-7.2).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkLoader(IFormFile csvFile)
        {
            var viewModel = new BulkImportViewModel();

            if (csvFile == null || csvFile.Length == 0)
            {
                viewModel.ErrorMessages.Add("Please upload a non-empty CSV file.");
                return View(viewModel);
            }

            var listingsToInsert = new List<CarListing>();
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };

            try
            {
                using var stream = csvFile.OpenReadStream();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, csvConfig);

                await csv.ReadAsync();
                csv.ReadHeader();

                int rowNumber = 1;
                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    try
                    {
                        var row = csv.GetRecord<CarListingCsvRow>();

                        if (string.IsNullOrWhiteSpace(row?.Make) || string.IsNullOrWhiteSpace(row?.Model))
                        {
                            viewModel.SkippedCount++;
                            viewModel.ErrorMessages.Add($"Row {rowNumber}: Make and Model are required.");
                            continue;
                        }

                        listingsToInsert.Add(new CarListing
                        {
                            Available = true,
                            ListingPrice = row.ListingPrice,
                            Car = new Car
                            {
                                Make = row.Make.Trim(),
                                Model = row.Model.Trim(),
                                Year = row.Year,
                                Trim = string.IsNullOrWhiteSpace(row.Trim) ? "Base" : row.Trim.Trim()
                            }
                        });
                    }
                    catch (CsvHelperException ex)
                    {
                        viewModel.SkippedCount++;
                        viewModel.ErrorMessages.Add($"Row {rowNumber}: {ex.Message.Split('\n')[0]}");
                    }
                }

                if (listingsToInsert.Count > 0)
                {
                    await _context.CarListings.AddRangeAsync(listingsToInsert);
                    await _context.SaveChangesAsync();
                    viewModel.InsertedCount = listingsToInsert.Count;
                }

                viewModel.IsSuccess = viewModel.InsertedCount > 0;
            }
            catch (Exception ex)
            {
                viewModel.IsSuccess = false;
                viewModel.ErrorMessages.Add($"Failed to read CSV: {ex.Message}");
            }

            return View(viewModel);
        }
    }
}