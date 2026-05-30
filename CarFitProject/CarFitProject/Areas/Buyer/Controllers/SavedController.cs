using CarFitProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarFitProject.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class SavedController : Controller
    {
        private readonly ISavedCarsService _saved;
        private readonly ISubscriptionService _subscriptions;

        public SavedController(ISavedCarsService saved, ISubscriptionService subscriptions)
        {
            _saved = saved;
            _subscriptions = subscriptions;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var listings = await _saved.GetSavedListingsAsync(UserId);
            ViewBag.IsPremium = await _subscriptions.IsPremiumAsync(UserId);
            ViewBag.FreeLimit = _subscriptions.FreeSaveLimit;
            ViewBag.SavedCount = listings.Count;
            return View(listings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int carId, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var result = await _saved.ToggleAsync(UserId, carId);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
