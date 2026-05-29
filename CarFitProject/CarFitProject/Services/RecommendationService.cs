using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public interface IRecommendationService
    {
        Task<List<RecommendedCarViewModel>> GetMatchesAsync(UserProfile profile);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly CarFitDbContext _context;

        public RecommendationService(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<List<RecommendedCarViewModel>> GetMatchesAsync(UserProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var budgetMin = profile.BudgetMin ?? 0m;
            var budgetMax = profile.BudgetMax;
            var transmissionPref = profile.TransmissionPref;
            var sizePref = profile.SizePref;

            var query = _context.VwAvailableCarDetails.AsNoTracking().AsQueryable();

            if (budgetMax > 0)
            {
                query = query.Where(c => c.ListingPrice >= budgetMin && c.ListingPrice <= budgetMax);
            }

            if (!string.IsNullOrWhiteSpace(transmissionPref))
            {
                query = query.Where(c => c.Transmission == null || c.Transmission == transmissionPref);
            }

            if (!string.IsNullOrWhiteSpace(sizePref))
            {
                query = query.Where(c => c.BodyType == null || c.BodyType.Contains(sizePref));
            }

            var rows = await query
                .OrderByDescending(c => c.TrustScore)
                .ThenBy(c => c.ListingPrice)
                .Take(50)
                .ToListAsync();

            return rows.Select(c => new RecommendedCarViewModel
            {
                CarId = c.CarId,
                Make = c.Make,
                Model = c.Model,
                Year = c.Year,
                ListingPrice = c.ListingPrice ?? 0m,
                City = c.City,
                BodyCondition = c.BodyCondition,
                DescriptionScore = c.DescriptionScore,
                TrustScore = c.TrustScore,
                DynamicMatchScore = 0
            }).ToList();
        }
    }
}
