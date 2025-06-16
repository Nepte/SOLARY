using SOLARY.Views;
using SOLARY.Services;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace SOLARY.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool _isNavigating = false;

        public ICommand MyAccountCommand { get; }
        public ICommand SolarPanelDetailsCommand { get; }
        public ICommand ContactUsCommand { get; }
        public ICommand TermsConditionsCommand { get; }
        public ICommand PrivacyPolicyCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand LogoutCommand { get; }

        // Navigation commands pour la bottom bar
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToStatsCommand { get; }
        public ICommand NavigateToMapCommand { get; }
        public ICommand NavigateToCodeCommand { get; }

        public SettingsViewModel()
        {
            MyAccountCommand = new Command(async () => await OnMyAccount());
            SolarPanelDetailsCommand = new Command(async () => await OnSolarPanelDetails());
            ContactUsCommand = new Command(async () => await OnContactUs());
            TermsConditionsCommand = new Command(async () => await OnTermsConditions());
            PrivacyPolicyCommand = new Command(async () => await OnPrivacyPolicy());
            AboutCommand = new Command(async () => await OnAbout());
            LogoutCommand = new Command(async () => await OnLogout());

            NavigateToHomeCommand = new Command(async () => await NavigateToHome());
            NavigateToStatsCommand = new Command(async () => await NavigateToStats());
            NavigateToMapCommand = new Command(async () => await NavigateToMap());
            NavigateToCodeCommand = new Command(async () => await NavigateToCode());
        }

        private async Task OnMyAccount()
        {
            Debug.WriteLine("[DEBUG] Mon compte cliqué");
            await ShowAlertAsync("Mon compte", "Fonctionnalité à configurer");
        }

        private async Task OnSolarPanelDetails()
        {
            if (_isNavigating) return;
            try
            {
                _isNavigating = true;
                Debug.WriteLine("[DEBUG] Détails du panneau solaire cliqué");
                var page = GetCurrentPage();
                if (page?.Navigation != null)
                {
                    await page.Navigation.PushAsync(new SolarPanelDetailsPage());
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async Task OnContactUs()
        {
            Debug.WriteLine("[DEBUG] Nous contacter cliqué");
            await ShowAlertAsync("Nous contacter", "Fonctionnalité à configurer");
        }

        private async Task OnTermsConditions()
        {
            Debug.WriteLine("[DEBUG] Termes et conditions cliqué");
            await ShowAlertAsync("Termes et conditions", "Fonctionnalité à configurer");
        }

        private async Task OnPrivacyPolicy()
        {
            Debug.WriteLine("[DEBUG] Politique de confidentialité cliqué");
            await ShowAlertAsync("Politique de confidentialité", "Fonctionnalité à configurer");
        }

        private async Task OnAbout()
        {
            Debug.WriteLine("[DEBUG] À propos cliqué");
            await ShowAlertAsync("À propos", "Fonctionnalité à configurer");
        }

        private async Task OnLogout()
        {
            if (_isNavigating) return;

            try
            {
                _isNavigating = true;
                Debug.WriteLine("[DEBUG] Déconnexion en cours...");

                // Confirmation de déconnexion
                var page = GetCurrentPage();
                if (page != null)
                {
                    bool confirm = await page.DisplayAlert(
                        "Déconnexion",
                        "Êtes-vous sûr de vouloir vous déconnecter ?",
                        "Oui",
                        "Annuler");

                    if (confirm)
                    {
                        // ✅ CORRIGÉ: Supprimer complètement la session "Se souvenir de moi"
                        try
                        {
                            SecureStorage.Remove("RememberMeSession");
                            Debug.WriteLine("[DEBUG] ✅ Session 'Se souvenir de moi' SUPPRIMÉE");

                            // Vérifier que c'est bien supprimé
                            var checkSession = await SecureStorage.GetAsync("RememberMeSession");
                            Debug.WriteLine($"[DEBUG] Vérification suppression: '{checkSession}' (doit être null/vide)");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[DEBUG] Erreur lors de la suppression de session RememberMe: {ex.Message}");
                        }

                        // Supprimer la session utilisateur actuelle avec la bonne méthode
                        SessionService.ClearSession();
                        Debug.WriteLine("[DEBUG] Session utilisateur supprimée");

                        // Navigation vers WelcomePage
                        await SafeNavigateToWelcome();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Erreur lors de la déconnexion: {ex.Message}");
                await ShowAlertAsync("Erreur", "Une erreur s'est produite lors de la déconnexion.");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async Task SafeNavigateToWelcome()
        {
            try
            {
                var page = GetCurrentPage();
                if (page?.Navigation != null)
                {
                    // Naviguer vers WelcomePage et vider la pile de navigation
                    await page.Navigation.PushAsync(new WelcomePage());

                    // Supprimer toutes les pages précédentes de la pile
                    var existingPages = page.Navigation.NavigationStack.ToList();
                    for (int i = existingPages.Count - 2; i >= 0; i--)
                    {
                        page.Navigation.RemovePage(existingPages[i]);
                    }

                    Debug.WriteLine("[DEBUG] Navigation vers WelcomePage réussie");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Erreur lors de la navigation vers WelcomePage: {ex.Message}");
            }
        }

        // Navigation commands pour la bottom bar
        private async Task NavigateToHome()
        {
            if (_isNavigating) return;
            try
            {
                _isNavigating = true;
                var page = GetCurrentPage();
                if (page?.Navigation != null)
                {
                    await page.Navigation.PushAsync(new HomePage());
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async Task NavigateToStats()
        {
            Debug.WriteLine("[DEBUG] Navigation vers Statistiques - À implémenter");
            await ShowAlertAsync("Statistiques", "Page à créer");
        }

        private async Task NavigateToMap()
        {
            if (_isNavigating) return;
            try
            {
                _isNavigating = true;
                var page = GetCurrentPage();
                if (page?.Navigation != null)
                {
                    await page.Navigation.PushAsync(new MapPage());
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async Task NavigateToCode()
        {
            if (_isNavigating) return;
            try
            {
                _isNavigating = true;
                var page = GetCurrentPage();
                if (page?.Navigation != null)
                {
                    await page.Navigation.PushAsync(new CodePage());
                }
            }
            finally
            {
                _isNavigating = false;
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
