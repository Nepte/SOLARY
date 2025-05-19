using SOLARY.Services;
using SOLARY.Views;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace SOLARY.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService;

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public ICommand LoginCommand { get; }
        public ICommand NavigateToRegisterCommand { get; }

        public LoginViewModel()
        {
            _authService = new AuthService();
            LoginCommand = new Command(async () => await Login());
            NavigateToRegisterCommand = new Command(async () => await NavigateToRegister());
        }

        private async Task Login()
        {
            var page = GetCurrentPage();
            if (page == null) return;

            if (!await ValidateInputs()) return;

            try
            {
                Debug.WriteLine($"[DEBUG] Tentative de connexion pour {Email}");
                var response = await _authService.Login(Email, Password);

                if (response == null)
                {
                    await ShowAlertAsync("Erreur Connexion", "Aucune réponse de l'API.");
                    return;
                }

                Debug.WriteLine($"[DEBUG] Réponse API Login: Success={response.Success}, Message={response.Message}");

                if (response.Success)
                {
                    await ShowAlertAsync("Succès", "Connexion réussie !");
                    await NavigateToHomePage(); // Navigation vers HomePage
                }
                else
                {
                    await ShowAlertAsync("Erreur", response.Message ?? "Identifiants incorrects ou compte non vérifié.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Exception lors de la connexion : {ex.Message}");
                await ShowAlertAsync("Erreur Exception", $"Une erreur s'est produite : {ex.Message}");
            }
        }

        private async Task<bool> ValidateInputs()
        {
            if (string.IsNullOrEmpty(Email) || !Email.Contains("@") || !Email.Contains("."))
            {
                await ShowAlertAsync("Erreur", "Adresse e-mail invalide.");
                return false;
            }

            if (string.IsNullOrEmpty(Password))
            {
                await ShowAlertAsync("Erreur", "Veuillez entrer un mot de passe.");
                return false;
            }

            return true;
        }

        private async Task NavigateToRegister()
        {
            var page = GetCurrentPage();
            if (page?.Navigation != null)
            {
                await page.Navigation.PushAsync(new RegisterPage());
            }
        }

        // Nouvelle méthode pour naviguer vers HomePage
        private async Task NavigateToHomePage()
        {
            var page = GetCurrentPage();
            if (page?.Navigation != null)
            {
                await page.Navigation.PushAsync(new HomePage());
            }
        }

        private async Task ShowAlertAsync(string title, string message)
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