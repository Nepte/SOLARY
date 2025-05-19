using SOLARY.Model;
using System.Threading.Tasks;

namespace SOLARY.Services
{
    public class AuthService : BaseApiService
    {
        public AuthService() : base("https://solary.vabre.ch/")
        {
        }

        // Inscription d'un utilisateur
        public async Task<ApiResponse?> Register(string email, string password, string confirmPassword)
        {
            var data = new { email, password, confirm_password = confirmPassword };
            return await PostAsync<ApiResponse>("register", data);
        }

        // Vérification du code OTP
        public async Task<ApiResponse?> VerifyOtp(string email, string otpCode)
        {
            var data = new { email, otp_code = otpCode };
            return await PostAsync<ApiResponse>("verify-otp", data);
        }

        // Renvoi du code OTP
        public async Task<ApiResponse?> ResendOtp(string email)
        {
            var data = new { email };
            return await PostAsync<ApiResponse>("resend-otp", data);
        }

        // Connexion
        public async Task<ApiResponse?> Login(string email, string password)
        {
            var data = new { email, password };
            return await PostAsync<ApiResponse>("login", data);
        }
    }
}
