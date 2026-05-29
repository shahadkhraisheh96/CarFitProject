using CarFitProject.Data;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace CarFitProject.Controllers
{
    public class InspectionController : Controller
    {
        // Change from ApplicationDbContext to CarFitDbContext to point to your main application data layer
        private readonly CarFitDbContext _context;

        public InspectionController(CarFitDbContext context)
        {
            _context = context;
        }

        // GET: /Inspection/
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Inspection/Book
        [HttpGet]
        public IActionResult Book()
        {
            return View(new InspectionBookingViewModel());
        }

        // POST: /Inspection/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(InspectionBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // 1. Map the ViewModel data explicitly over to a new Database Entity object
                var bookingEntity = new InspectionBooking
                {
                    CustomerName = model.CustomerName,
                    CustomerEmail = model.CustomerEmail,
                    PackageType = model.PackageType,
                    PreferredDate = model.PreferredDate,
                    VehicleNotes = model.VehicleNotes,
                    CreatedAt = DateTime.UtcNow
                };

                // 2. State tracking mutation & database commit execution pipelines
                _context.InspectionBookings.Add(bookingEntity);
                await _context.SaveChangesAsync();

                // 3. Set confirmation triggers and redirect to prevent duplicate submissions on refresh
                TempData["SuccessMessage"] = "Your CarFit evaluation request has been processed successfully!";
                return RedirectToAction(nameof(Book));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unhandled issue occurred while saving your data to SQL Server. Please try again.");
                return View(model);
            }
        }
    }
}