using CarFitProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public interface ISubscriptionService
    {
        int FreeSaveLimit { get; }
        bool IsPremium(ApplicationUser? user);
        Task<bool> IsPremiumAsync(string? userId);
        Task<bool> CanSaveMoreAsync(string? userId);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private const int FreeLimit = 3;

        private readonly CarFitDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubscriptionService(CarFitDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int FreeSaveLimit => FreeLimit;

        public bool IsPremium(ApplicationUser? user)
            => user != null
               && string.Equals(user.SubscriptionTier, "Premium", StringComparison.OrdinalIgnoreCase);

        public async Task<bool> IsPremiumAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            return IsPremium(user);
        }

        public async Task<bool> CanSaveMoreAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            if (await IsPremiumAsync(userId)) return true;
            var count = await _context.SavedResults.CountAsync(s => s.UserId == userId);
            return count < FreeLimit;
        }
    }
}
