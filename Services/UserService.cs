using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOLARY.Model;

namespace SOLARY.Services
{
    public class UserService : BaseApiService, IUserService
    {
        public UserService() : base("https://solary.vabre.ch/")
        {
        }

        // Récupérer un utilisateur par son ID
        public async Task<User?> GetUserById(int userId)
        {
            try
            {
                var user = await GetAsync<User>($"GetUser/{userId}");
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération de l'utilisateur: {ex.Message}");
                return null;
            }
        }

        // Mettre à jour le code de casier d'un utilisateur
        public async Task<bool> UpdateLockerCode(int userId, string newCode)
        {
            try
            {
                if (string.IsNullOrEmpty(newCode) || newCode.Length < 4 || newCode.Length > 10)
                {
                    return false;
                }

                var data = new
                {
                    code_casiers = newCode
                };

                System.Diagnostics.Debug.WriteLine($"[DEBUG] PUT -> UpdateUserCode/{userId}, data = {System.Text.Json.JsonSerializer.Serialize(data)}");

                var response = await PutAsync<ApiResponse>($"UpdateUserCode/{userId}", data);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Réponse API: Success = {response?.Success}, Message = {response?.Message}");

                // Vérifier plusieurs conditions de succès
                if (response != null)
                {
                    // Condition 1: Success explicite
                    if (response.Success == true)
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Code mis à jour avec succès (Success = true)");
                        return true;
                    }

                    // Condition 2: Message contient "succès"
                    if (!string.IsNullOrEmpty(response.Message) &&
                        (response.Message.Contains("succès") || response.Message.Contains("success")))
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Code mis à jour avec succès (message de succès détecté)");
                        return true;
                    }

                    // Condition 3: Pas d'erreur explicite et réponse non-null
                    if (string.IsNullOrEmpty(response.Message) ||
                        (!response.Message.Contains("erreur") && !response.Message.Contains("error")))
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Code mis à jour avec succès (pas d'erreur détectée)");
                        return true;
                    }
                }

                System.Diagnostics.Debug.WriteLine("[DEBUG] Échec de la mise à jour du code");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour du code casier: {ex.Message}");
                return false;
            }
        }

        // Vérifier si un code de casier est valide pour un utilisateur donné
        public async Task<bool> ValidateLockerCode(int userId, string code)
        {
            try
            {
                var user = await GetUserById(userId);
                return user != null && user.CodeCasiers == code;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la validation du code casier: {ex.Message}");
                return false;
            }
        }

        // Récupérer un utilisateur par son email
        public async Task<User?> GetUserByEmail(string email)
        {
            try
            {
                // Si votre API a un endpoint GetUserByEmail, utilisez-le
                // Sinon, on peut récupérer tous les utilisateurs et filtrer (moins efficace)

                // Pour le moment, on va essayer l'endpoint direct
                var user = await GetAsync<User>($"GetUserByEmail/{Uri.EscapeDataString(email)}");
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération de l'utilisateur par email: {ex.Message}");
                return null;
            }
        }
    }
}
