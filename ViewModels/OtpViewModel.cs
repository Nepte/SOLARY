using SOLARY.Services;
using SOLARY.Views;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace SOLARY.ViewModels
{
    public class OtpViewModel : BaseViewModel
    {
        private readonly string _email;
        private readonly AuthService _authService;

        public string OtpCode1 { get; set; } = string.Empty;
        public string OtpCode2 { get; set; } = string.Empty;
        public string OtpCode3 { get; set; } = string.Empty;
        public string OtpCode4 { get; set; } = string.Empty;
        public string OtpCode5 { get; set; } = string.Empty;
        public string OtpCode6 { get; set; } = string.Empty;

        public ICommand ValidateOtpCommand { get; }
        public ICommand ResendOtpCommand { get; }

        public OtpViewModel(string email)
        {
            _email = email;
            _authService = new AuthService();

            ValidateOtpCommand = new Command(async () => await ValidateOtp());
            ResendOtpCommand = new Command(async () => await ResendOtp());
        }

        private async Task ValidateOtp()
        {
            // Concaténer les 6 caractères
            var fullOtp = $"{OtpCode1}{OtpCode2}{OtpCode3}{OtpCode4}{OtpCode5}{OtpCode6}";

            if (fullOtp.Length < 6)
            {
                await ShowAlertAsync("Erreur", "Veuillez entrer les 6 chiffres de votre code OTP.");
                return;
            }

            try
            {
                Console.WriteLine($"[DEBUG] Vérification OTP pour {_email} avec code {fullOtp}");
                var response = await _authService.VerifyOtp(_email, fullOtp);

                if (response == null)
                {
                    await ShowAlertAsync("Erreur", "Aucune réponse de l'API.");
                    return;
                }

                Console.WriteLine($"[DEBUG] Réponse API OTP: Success={response.Success}, Message={response.Message}");

                if (response.Success)
                {
                    await ShowAlertAsync("Succès", "Code validé. Votre compte est maintenant actif !");
                    await NavigateToMainPage();
                }
                else
                {
                    await ShowAlertAsync("Erreur", response.Message ?? "Code incorrect.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] Exception lors de la validation OTP: {ex.Message}");
                await ShowAlertAsync("Erreur Exception", $"Une erreur s'est produite : {ex.Message}");
            }
        }

        private async Task ResendOtp()
        {
            try
            {
                Console.WriteLine($"[DEBUG] Renvoi OTP pour {_email}");
                var response = await _authService.ResendOtp(_email);

                if (response == null)
                {
                    await ShowAlertAsync("Erreur", "Aucune réponse de l'API.");
                    return;
                }

                Console.WriteLine($"[DEBUG] Réponse API Renvoi OTP: Success={response.Success}, Message={response.Message}");

                await ShowAlertAsync(response.Success ? "Info" : "Erreur", response.Message ?? "Une erreur est survenue.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR] Exception lors du renvoi OTP: {ex.Message}");
                await ShowAlertAsync("Erreur Exception", $"Une erreur s'est produite : {ex.Message}");
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

        private async Task NavigateToMainPage()
        {
            var page = GetCurrentPage();
            if (page?.Navigation != null)
            {
                await page.Navigation.PushAsync(new MainPage());
            }
        }

        private Page? GetCurrentPage()
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page;
        }
    }
}