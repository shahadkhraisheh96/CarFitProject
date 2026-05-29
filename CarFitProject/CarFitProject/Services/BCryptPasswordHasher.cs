using Microsoft.AspNetCore.Identity;

namespace CarFitProject.Services
{
    public class BCryptPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        private const int WorkFactor = 12;

        public string HashPassword(TUser user, string password)
            => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
                return PasswordVerificationResult.Failed;

            try
            {
                return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
                    ? PasswordVerificationResult.Success
                    : PasswordVerificationResult.Failed;
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return PasswordVerificationResult.Failed;
            }
        }
    }
}
