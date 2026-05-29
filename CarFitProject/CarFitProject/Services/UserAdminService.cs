using CarFitProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public interface IUserAdminService
    {
        Task<DeleteUserResult> DeleteUserAsync(string userId);
    }

    public record DeleteUserResult(bool Ok, string Message);

    public class UserAdminService : IUserAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CarFitDbContext _carFitContext;
        private readonly IConfiguration _config;

        public UserAdminService(
            UserManager<ApplicationUser> userManager,
            CarFitDbContext carFitContext,
            IConfiguration config)
        {
            _userManager = userManager;
            _carFitContext = carFitContext;
            _config = config;
        }

        public async Task<DeleteUserResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new DeleteUserResult(false, "User not found.");
            }

            var seededAdminEmail = _config["AdminSeed:Email"];
            if (!string.IsNullOrEmpty(seededAdminEmail)
                && string.Equals(user.Email, seededAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                return new DeleteUserResult(false, "The seeded admin account cannot be deleted.");
            }

            // If the user is a dealer with listings, block deletion. Deactivation is
            // the right tool there — it preserves listing history. Otherwise drop
            // the Seller row first so the user delete can succeed cleanly.
            var seller = await _carFitContext.Sellers
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (seller != null)
            {
                var hasListings = await _carFitContext.CarListings.AnyAsync(l => l.SellerId == seller.Id);
                if (hasListings)
                {
                    return new DeleteUserResult(false,
                        $"{user.Email} is a dealer with existing listings. Deactivate the account instead — deleting would orphan listing history.");
                }

                _carFitContext.Sellers.Remove(seller);
                await _carFitContext.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return new DeleteUserResult(false, $"Identity refused to delete the user: {errors}");
            }

            return new DeleteUserResult(true, $"Deleted user {user.Email}.");
        }
    }
}
