using CarFitProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public sealed record SaveToggleResult(bool Ok, bool IsSaved, string Message);

    public interface ISavedCarsService
    {
        Task<SaveToggleResult> ToggleAsync(string userId, int carId);
        Task<HashSet<int>> GetSavedCarIdsAsync(string userId);
        Task<List<CarListing>> GetSavedListingsAsync(string userId);
        Task<int> CountAsync(string userId);
    }

    public class SavedCarsService : ISavedCarsService
    {
        private readonly CarFitDbContext _context;
        private readonly ISubscriptionService _subscriptions;

        public SavedCarsService(CarFitDbContext context, ISubscriptionService subscriptions)
        {
            _context = context;
            _subscriptions = subscriptions;
        }

        public async Task<SaveToggleResult> ToggleAsync(string userId, int carId)
        {
            if (string.IsNullOrEmpty(userId))
                return new SaveToggleResult(false, false, "Sign in to save cars.");

            var existing = await _context.SavedResults
                .FirstOrDefaultAsync(s => s.UserId == userId && s.CarId == carId);

            if (existing != null)
            {
                _context.SavedResults.Remove(existing);
                await _context.SaveChangesAsync();
                return new SaveToggleResult(true, false, "Removed from saved cars.");
            }

            if (!await _subscriptions.CanSaveMoreAsync(userId))
            {
                return new SaveToggleResult(false, false,
                    $"You're at the {_subscriptions.FreeSaveLimit}-car limit on the Free plan — upgrade to Premium to save more.");
            }

            _context.SavedResults.Add(new SavedResult
            {
                UserId = userId,
                CarId = carId
            });
            await _context.SaveChangesAsync();
            return new SaveToggleResult(true, true, "Saved.");
        }

        public async Task<HashSet<int>> GetSavedCarIdsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new HashSet<int>();
            var ids = await _context.SavedResults
                .Where(s => s.UserId == userId)
                .Select(s => s.CarId)
                .ToListAsync();
            return new HashSet<int>(ids);
        }

        public async Task<List<CarListing>> GetSavedListingsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<CarListing>();

            var ids = await _context.SavedResults
                .Where(s => s.UserId == userId)
                .Select(s => s.CarId)
                .ToListAsync();
            if (ids.Count == 0) return new List<CarListing>();

            return await _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .Where(l => ids.Contains(l.CarId!.Value) && l.Status == "Active")
                .ToListAsync();
        }

        public Task<int> CountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return Task.FromResult(0);
            return _context.SavedResults.CountAsync(s => s.UserId == userId);
        }
    }
}
