using SOLARY.Services;
using SOLARY.Views;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace SOLARY.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly AuthService _authService;

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool IsTermsAccepted { get; set; }

        public ICommand RegisterCommand { get; }

        public RegisterViewModel()
        {
            _authService = new AuthService();
            RegisterCommand = new Command(async () => await Register());
        }

        private async Task Register()
        {
            var page = GetCurrentPage();
            if (page == null) return;

            if (!await ValidateInputs()) return;

            try
            {
                Debug.WriteLine("[DEBUG] Tentative d'inscription pour " + Email);

                // Appel à l'API /register
                var response = await _authService.Register(Email, Password, ConfirmPassword);

                if (response == null)
                {
                    await ShowAlert("Erreur Inscription", "Aucune réponse de l'API.");
                    Debug.WriteLine("[ERREUR] Réponse API NULL");
                    return;
                }

                Debug.WriteLine($"[DEBUG] Réponse API: Success={response.Success}, Message={response.Message}");

                if (!response.Success)
                {
                    // En cas d'échec, on affiche le message d'erreur
                    await ShowAlert("Erreur Inscription", response.Message ?? "Une erreur est survenue.");
                    Debug.WriteLine($"[ERREUR] Message API: {response.Message}");
                    return;
                }

                // Si tout est OK, le serveur a déjà envoyé l'OTP par mail
                await ShowAlert("Succès", "Compte créé avec succès. Un code OTP vous a été envoyé par e-mail.");

                // Navigation directe vers la page OTP en passant l'email
                await NavigateToPage(new OtpPage(Email));
            }
            catch (Exception ex)
            {
                await ShowAlert("Erreur Exception", $"Exception : {ex.Message}");
                Debug.WriteLine($"[ERREUR EXCEPTION] {ex}");
            }
        }

        private async Task<bool> ValidateInputs()
        {
            // Vérifications basiques (email valide, passwords, etc.)
            if (string.IsNullOrEmpty(Email) || !Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                await ShowAlert("Erreur", "Adresse e-mail invalide.");
                return false;
            }

            if (string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(ConfirmPassword))
            {
                await ShowAlert("Erreur", "Veuillez remplir tous les champs.");
                return false;
            }

            if (Password != ConfirmPassword)
            {
                await ShowAlert("Erreur", "Les mots de passe ne correspondent pas.");
                return false;
            }

            if (!IsTermsAccepted)
            {
                await ShowAlert("Erreur", "Vous devez accepter les termes et conditions.");
                return false;
            }

            return true;
        }

        private async Task NavigateToPage(Page page)
        {
            var currentPage = GetCurrentPage();
            if (currentPage?.Navigation != null)
            {
                await currentPage.Navigation.PushAsync(page);
            }
        }

        private async Task ShowAlert(string title, string message)
        {
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlert(title, message, "OK");
            }
        }

        private Page? GetCurrentPage()
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page;
        }
    }
}