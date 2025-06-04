using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace SOLARY.Services
{
    public static class SessionService
    {
        private const string USER_ID_KEY = "current_user_id";
        private const string USER_EMAIL_KEY = "current_user_email";

        // Propriété pour récupérer l'ID de l'utilisateur connecté
        public static int? CurrentUserId
        {
            get
            {
                var userIdString = Preferences.Get(USER_ID_KEY, string.Empty);
                if (int.TryParse(userIdString, out int userId))
                {
                    return userId;
                }
                return null;
            }
        }

        // Propriété pour récupérer l'email de l'utilisateur connecté
        public static string? CurrentUserEmail
        {
            get => Preferences.Get(USER_EMAIL_KEY, string.Empty);
        }

        // Vérifier si un utilisateur est connecté
        public static bool IsLoggedIn => CurrentUserId.HasValue;

        // Sauvegarder la session après connexion
        public static void SaveUserSession(int userId, string email)
        {
            Preferences.Set(USER_ID_KEY, userId.ToString());
            Preferences.Set(USER_EMAIL_KEY, email);
            Debug.WriteLine($"[SessionService] Session sauvegardée - UserId: {userId}, Email: {email}");
        }

        // Effacer la session (déconnexion)
        public static void ClearSession()
        {
            Preferences.Remove(USER_ID_KEY);
            Preferences.Remove(USER_EMAIL_KEY);
            Debug.WriteLine("[SessionService] Session effacée");
        }

        // Méthode pour récupérer l'ID utilisateur avec une valeur par défaut
        public static int GetCurrentUserIdOrDefault(int defaultValue = 0)
        {
            return CurrentUserId ?? defaultValue;
        }
    }
}
