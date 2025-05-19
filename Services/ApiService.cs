using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SOLARY.Services
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        // Si vous voulez récupérer d'autres champs (ex: user, token...), ajoutez-les
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            var handler = new HttpClientHandler
            {
                // Pour ignorer les certificats en dev
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://solary.vabre.ch/"), // L'URL de base de votre API
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        // Méthode générique pour faire un POST JSON
        private async Task<ApiResponse> PostAsync(string endpoint, object data)
        {
            try
            {
                // 1) Sérialisation en JSON
                string json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"[DEBUG] POST -> {endpoint}, data = {json}");

                // 2) Appel HTTP POST
                var response = await _httpClient.PostAsync(endpoint, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[DEBUG] Réponse brute : {responseContent}");

                // 3) Vérifications
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return new ApiResponse { Success = false, Message = "Réponse vide de l'API." };
                }
                if (responseContent.TrimStart().StartsWith("<"))
                {
                    return new ApiResponse { Success = false, Message = "Erreur serveur (HTML reçu)." };
                }

                // 4) Désérialisation
                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent,
                                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse == null)
                {
                    return new ApiResponse { Success = false, Message = "Réponse JSON invalide." };
                }

                return apiResponse;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[ERREUR] Connexion API : {ex.Message}");
                return new ApiResponse { Success = false, Message = $"Problème de connexion : {ex.Message}" };
            }
            catch (TaskCanceledException)
            {
                return new ApiResponse { Success = false, Message = "Timeout. Le serveur ne répond pas." };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Exception générale : {ex.Message}");
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        // (A) Register
        public async Task<ApiResponse> Register(string email, string password, string confirmPassword)
        {
            var data = new { email, password, confirm_password = confirmPassword };
            // Note : côté API, vous n'utilisez pas confirm_password. Soit vous l'enlevez, soit l'API l'ignore.
            return await PostAsync("register", data);
        }

        // (B) Vérifier OTP
        public async Task<ApiResponse> VerifyOtp(string email, string otpCode)
        {
            var data = new { email, otp_code = otpCode };
            return await PostAsync("verify-otp", data);
        }

        // (C) Renvoyer OTP
        public async Task<ApiResponse> ResendOtp(string email)
        {
            var data = new { email };
            return await PostAsync("resend-otp", data);
        }

        // (D) Connexion
        public async Task<ApiResponse> Login(string email, string password)
        {
            var data = new { email, password };
            return await PostAsync("login", data);
        }
    }
}