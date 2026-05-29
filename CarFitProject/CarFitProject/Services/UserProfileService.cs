using System.Text.Json;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public interface IUserProfileService
    {
        ProfileDraftViewModel GetDraft(HttpContext httpContext);
        void SaveDraft(HttpContext httpContext, ProfileDraftViewModel draft);
        void ClearDraft(HttpContext httpContext);
        Task<ProfileDraftViewModel> StartFromProfileAsync(HttpContext httpContext, string userId, int profileId);
        Task<UserProfile?> CompleteAsync(string userId, ProfileDraftViewModel draft);
    }

    public class UserProfileService : IUserProfileService
    {
        private const string SessionKey = "buyer.profile.draft";

        private readonly CarFitDbContext _context;

        public UserProfileService(CarFitDbContext context)
        {
            _context = context;
        }

        public ProfileDraftViewModel GetDraft(HttpContext httpContext)
        {
            var json = httpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return new ProfileDraftViewModel { ProfileName = "My Fit Profile" };
            }

            try
            {
                return JsonSerializer.Deserialize<ProfileDraftViewModel>(json)
                    ?? new ProfileDraftViewModel { ProfileName = "My Fit Profile" };
            }
            catch (JsonException)
            {
                return new ProfileDraftViewModel { ProfileName = "My Fit Profile" };
            }
        }

        public void SaveDraft(HttpContext httpContext, ProfileDraftViewModel draft)
        {
            httpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(draft));
        }

        public void ClearDraft(HttpContext httpContext)
        {
            httpContext.Session.Remove(SessionKey);
        }

        public async Task<ProfileDraftViewModel> StartFromProfileAsync(HttpContext httpContext, string userId, int profileId)
        {
            var existing = await _context.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProfileId == profileId && p.UserId == userId);

            var draft = existing == null
                ? new ProfileDraftViewModel { ProfileName = "My Fit Profile" }
                : new ProfileDraftViewModel
                {
                    ExistingProfileId = existing.ProfileId,
                    ProfileName = existing.ProfileName,
                    Age = existing.Age,
                    MaritalStatus = existing.MaritalStatus,
                    HasKids = existing.HasKids,
                    KidsCount = existing.KidsCount,
                    Purpose = existing.Purpose,
                    TripType = existing.TripType,
                    BudgetMin = existing.BudgetMin,
                    BudgetMax = existing.BudgetMax,
                    PaymentMethod = existing.PaymentMethod,
                    InstallmentMonths = existing.InstallmentMonths,
                    ConditionPref = existing.ConditionPref,
                    TransmissionPref = existing.TransmissionPref,
                    SizePref = existing.SizePref
                };

            SaveDraft(httpContext, draft);
            return draft;
        }

        public async Task<UserProfile?> CompleteAsync(string userId, ProfileDraftViewModel draft)
        {
            UserProfile? profile;
            if (draft.ExistingProfileId.HasValue)
            {
                profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.ProfileId == draft.ExistingProfileId.Value && p.UserId == userId);
                if (profile == null) return null;
            }
            else
            {
                profile = new UserProfile
                {
                    UserId = userId,
                    ProfileName = "My Fit Profile",
                    TransmissionPref = "Automatic",
                    SizePref = "Medium",
                    BudgetMax = 0m
                };
                _context.UserProfiles.Add(profile);
            }

            profile.ProfileName = string.IsNullOrWhiteSpace(draft.ProfileName) ? "My Fit Profile" : draft.ProfileName.Trim();
            profile.Age = draft.Age;
            profile.MaritalStatus = draft.MaritalStatus;
            profile.HasKids = draft.HasKids;
            profile.KidsCount = draft.HasKids == true ? draft.KidsCount : 0;
            profile.Purpose = draft.Purpose;
            profile.TripType = draft.TripType;
            profile.BudgetMin = draft.BudgetMin;
            profile.BudgetMax = draft.BudgetMax ?? 0m;
            profile.PaymentMethod = draft.PaymentMethod;
            profile.InstallmentMonths = draft.PaymentMethod == "Installments" ? draft.InstallmentMonths : null;
            profile.ConditionPref = draft.ConditionPref;
            profile.TransmissionPref = draft.TransmissionPref ?? "Automatic";
            profile.SizePref = draft.SizePref ?? "Medium";
            profile.IsActive = true;

            await _context.SaveChangesAsync();
            return profile;
        }
    }
}
