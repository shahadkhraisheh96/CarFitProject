using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public interface IRecommendationService
    {
        Task<List<RecommendedCarViewModel>> GetMatchesForUserAsync(int userId);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly CarFitDbContext _context;

        public RecommendationService(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<List<RecommendedCarViewModel>> GetMatchesForUserAsync(int userId)
        {
            // Leverages EF Core 8+ raw SQL model mapping to invoke your stored procedure
            return await _context.Database
                .SqlQueryRaw<RecommendedCarViewModel>("EXEC GetCarMatchesForUser {0}", userId)
                .ToListAsync();
        }
    }
}
