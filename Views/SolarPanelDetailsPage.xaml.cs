using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;

namespace SOLARY.Views
{
    public partial class SolarPanelDetailsPage : ContentPage
    {
        public SolarPanelDetailsPage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Alexandre COENE
        private async void OnAlexandreLinkedInClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://www.linkedin.com/in/alexandre-coene-190105290/");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture LinkedIn Alexandre: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le lien LinkedIn.", "OK");
            }
        }

        private async void OnAlexandreEmailClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("mailto:a_coene@orange.fr");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture email Alexandre: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir l'application email.", "OK");
            }
        }

        private async void OnAlexandreGitHubClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://github.com/AlexandreCoene");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture GitHub Alexandre: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le lien GitHub.", "OK");
            }
        }

        // Clément VABRE
        private async void OnClementLinkedInClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://www.linkedin.com/in/clement-vabre-13742a262/");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture LinkedIn Clément: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le lien LinkedIn.", "OK");
            }
        }

        private async void OnClementPortfolioClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://portfolio.vabre.ch/");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture Portfolio Clément: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le portfolio.", "OK");
            }
        }

        private async void OnClementGitHubClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://github.com/ClementV74");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture GitHub Clément: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le lien GitHub.", "OK");
            }
        }

        // Yael ELKAROUI
        private async void OnYaelLinkedInClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://www.linkedin.com/in/yael-el-karoui-177776307/");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture LinkedIn Yael: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le lien LinkedIn.", "OK");
            }
        }

        private async void OnYaelGitHubClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://github.com/yael-ops");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture GitHub Yael: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le lien GitHub.", "OK");
            }
        }

        // Erzen GASHI
        private async void OnErzenLinkedInClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://www.linkedin.com/in/erzen-gashi/");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture LinkedIn Erzen: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le lien LinkedIn.", "OK");
            }
        }

        private async void OnErzenGitHubClicked(object sender, EventArgs e)
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync("https://github.com/Nepte");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture GitHub Erzen: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir le lien GitHub.", "OK");
            }
        }

        // Localisation
        private async void OnOpenMapsClicked(object sender, EventArgs e)
        {
            try
            {
                string address = "27 Fbg des Balmettes, 74000 Annecy, France";
                string encodedAddress = Uri.EscapeDataString(address);

                // Essayer Google Maps d'abord
                string mapsUrl = $"https://www.google.com/maps/search/?api=1&query={encodedAddress}";

                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(mapsUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur ouverture Maps: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible d'ouvrir l'application de cartes.", "OK");
            }
        }
    }
}
