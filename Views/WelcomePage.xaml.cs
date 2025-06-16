using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SOLARY.Views
{
    public partial class WelcomePage : ContentPage
    {
        private readonly List<string> _images = new() { "empreinte_carbone.png", "test.png", "chat.png" };
        private int _currentIndex = 0;
        private bool _isNavigating = false;
        private bool _hasCheckedAutoLogin = false; // ✨ Flag pour éviter les vérifications multiples

        public ICommand NavigateToLoginCommand { get; }

        public WelcomePage()
        {
            InitializeComponent();

            // Masquer complètement la barre de statut
            ConfigureFullScreen();

            // Initialiser la commande
            NavigateToLoginCommand = new Command(async () => await NavigateToLoginPage());
            BindingContext = this;

            // Adapter les tailles en fonction de l'écran
            AdaptToScreenSize();

            SetupImageContainer();
            StartImageRotationAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Forcer le plein écran à chaque apparition
            ConfigureFullScreen();

            // Réinitialiser le flag de navigation
            _isNavigating = false;

            // ✨ NOUVEAU: Vérifier l'auto-login dès l'apparition de WelcomePage
            if (!_hasCheckedAutoLogin)
            {
                _hasCheckedAutoLogin = true;
                await CheckAutoLoginAndSkip();
            }
        }

        // ✨ NOUVEAU: Vérifier l'auto-login et skip WelcomePage si nécessaire
        private async Task CheckAutoLoginAndSkip()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] WelcomePage: Vérification auto-login...");

                // Vérifier si l'utilisateur a une session "Se souvenir de moi"
                var savedSession = await SecureStorage.GetAsync("RememberMeSession");

                // ✅ CORRIGÉ: Vérifier que la session n'est pas vide ou null
                if (!string.IsNullOrEmpty(savedSession) && savedSession.Trim() != "")
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] WelcomePage: Session trouvée, parsing...");

                    var sessionParts = savedSession.Split('|');
                    if (sessionParts.Length >= 3)
                    {
                        var userId = sessionParts[0];
                        var email = sessionParts[1];

                        // ✅ CORRIGÉ: Vérifier que les données ne sont pas vides
                        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email))
                        {
                            var expirationDate = DateTime.Parse(sessionParts[2]);

                            // Vérifier si la session n'a pas expiré
                            if (DateTime.Now < expirationDate)
                            {
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] WelcomePage: Session valide pour {email}, skip vers HomePage...");

                                // Restaurer la session
                                SOLARY.Services.SessionService.SaveUserSession(int.Parse(userId), email);

                                // ✨ SKIP directement vers HomePage depuis WelcomePage
                                await SkipToHomePage();
                                return;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[DEBUG] WelcomePage: Session expirée, suppression...");
                                SecureStorage.Remove("RememberMeSession");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[DEBUG] WelcomePage: Données de session invalides, suppression...");
                            SecureStorage.Remove("RememberMeSession");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] WelcomePage: Format de session invalide, suppression...");
                        SecureStorage.Remove("RememberMeSession");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] WelcomePage: Pas de session RememberMe trouvée");
                }

                System.Diagnostics.Debug.WriteLine("[DEBUG] WelcomePage: Pas de session valide, affichage normal");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] WelcomePage: Erreur auto-login: {ex.Message}");
                // En cas d'erreur, supprimer la session corrompue
                try
                {
                    SecureStorage.Remove("RememberMeSession");
                }
                catch { }
            }
        }

        // ✨ NOUVEAU: Skip directement vers HomePage
        private async Task SkipToHomePage()
        {
            if (_isNavigating) return;

            try
            {
                _isNavigating = true;
                System.Diagnostics.Debug.WriteLine("[DEBUG] WelcomePage: Navigation directe vers HomePage...");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Navigation.PushAsync(new HomePage());
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERREUR] WelcomePage: Erreur navigation HomePage: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private void ConfigureFullScreen()
        {
            try
            {
                // Pour Android
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
#if ANDROID
                    var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                    if (activity?.Window != null)
                    {
                        // Rendre la barre d'état transparente
                        activity.Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
                        
                        // Configuration moderne pour Android 11+
                        if (OperatingSystem.IsAndroidVersionAtLeast(30))
                        {
                            ConfigureModernFullScreen(activity);
                        }
                        else
                        {
                            ConfigureLegacyFullScreen(activity);
                        }
                    }
#endif
                }
                // Pour iOS, la configuration est dans Info.plist
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration plein écran: {ex.Message}");
            }
        }

#if ANDROID
        private static void ConfigureModernFullScreen(Android.App.Activity activity)
        {
            try
            {
                var window = activity.Window;
                if (window != null)
                {
                    // Utiliser la réflexion pour WindowInsetsController
                    var insetsControllerProperty = window.GetType().GetProperty("InsetsController");
                    if (insetsControllerProperty != null)
                    {
                        var insetsController = insetsControllerProperty.GetValue(window);
                        if (insetsController != null)
                        {
                            var insetsControllerType = insetsController.GetType();
                            
                            // Masquer la barre d'état
                            var hideMethod = insetsControllerType.GetMethod("Hide");
                            if (hideMethod != null)
                            {
                                hideMethod.Invoke(insetsController, new object[] { 1 }); // 1 = StatusBars
                            }
                            
                            // Définir le comportement
                            var setBehaviorProperty = insetsControllerType.GetProperty("SystemBarsBehavior");
                            if (setBehaviorProperty != null)
                            {
                                setBehaviorProperty.SetValue(insetsController, 1); // BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                            }
                        }
                    }
                    
                    // SetDecorFitsSystemWindows
                    var setDecorFitsMethod = window.GetType().GetMethod("SetDecorFitsSystemWindows");
                    if (setDecorFitsMethod != null)
                    {
                        setDecorFitsMethod.Invoke(window, new object[] { false });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur avec WindowInsetsController: {ex.Message}");
                ConfigureLegacyFullScreen(activity);
            }
        }

        private static void ConfigureLegacyFullScreen(Android.App.Activity activity)
        {
            try
            {
                var window = activity.Window;
                if (window?.DecorView != null)
                {
#pragma warning disable CA1422 // Validate platform compatibility
#pragma warning disable CS0618 // Type or member is obsolete
                    window.DecorView.SystemUiFlags = (Android.Views.SystemUiFlags)(
                        Android.Views.SystemUiFlags.LayoutFullscreen | 
                        Android.Views.SystemUiFlags.LayoutStable |
                        Android.Views.SystemUiFlags.Fullscreen |
                        Android.Views.SystemUiFlags.ImmersiveSticky |
                        Android.Views.SystemUiFlags.HideNavigation |
                        Android.Views.SystemUiFlags.LayoutHideNavigation);
#pragma warning restore CS0618
#pragma warning restore CA1422
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur avec SystemUiFlags: {ex.Message}");
            }
        }
#endif

        private void AdaptToScreenSize()
        {
            try
            {
                // Obtenir les dimensions de l'écran
                var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                var screenHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;

                System.Diagnostics.Debug.WriteLine($"Taille écran: {screenWidth}x{screenHeight}");

                // Adapter les tailles en fonction de la taille de l'écran
                if (screenWidth < 360) // Petit écran
                {
                    // Réduire les tailles de police
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var label in this.GetVisualTreeDescendants().OfType<Label>())
                        {
                            if (label.FontSize > 30)
                                label.FontSize = 28;
                            else if (label.FontSize > 15)
                                label.FontSize = 14;
                        }

                        // Réduire la taille des barres de progression
                        var bar1 = this.FindByName<BoxView>("Bar1");
                        var bar2 = this.FindByName<BoxView>("Bar2");
                        var bar3 = this.FindByName<BoxView>("Bar3");

                        if (bar1 != null) bar1.WidthRequest = 60;
                        if (bar2 != null) bar2.WidthRequest = 60;
                        if (bar3 != null) bar3.WidthRequest = 60;
                    });
                }

                // Adapter la taille du conteneur d'image
                var imageContainer = this.FindByName<Border>("ImageContainer");
                if (imageContainer != null)
                {
                    var maxSize = Math.Min(300, screenWidth * 0.7); // Réduire la taille max
                    imageContainer.WidthRequest = maxSize;
                    imageContainer.HeightRequest = maxSize;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'adaptation à la taille de l'écran: {ex.Message}");
            }
        }

        private async Task NavigateToLoginPage()
        {
            if (_isNavigating) return;

            try
            {
                _isNavigating = true;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Navigation vers LoginPage...");
                await Navigation.PushAsync(new LoginPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERREUR] Erreur lors de la navigation: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private void SetupImageContainer()
        {
            var imageContainer = this.FindByName<Grid>("ImageContainer");
            if (imageContainer != null)
            {
                // Créer l'image avec des dimensions fixes pour éviter les erreurs Glide
                var image = new Image
                {
                    Source = _images[_currentIndex],
                    Aspect = Aspect.AspectFit,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    WidthRequest = 250, // Taille fixe
                    HeightRequest = 250 // Taille fixe
                };

                imageContainer.Children.Add(image);
                UpdateProgressBars(_currentIndex);
            }
        }

        private async void StartImageRotationAsync()
        {
            while (true)
            {
                await Task.Delay(1800);

                _currentIndex = (_currentIndex + 1) % _images.Count;
                await TransitionToNextImage(_images[_currentIndex]);
            }
        }

        private async Task TransitionToNextImage(string nextImageSource)
        {
            var imageContainer = this.FindByName<Grid>("ImageContainer");
            if (imageContainer != null)
            {
                var currentImage = imageContainer.Children.FirstOrDefault() as VisualElement;
                if (currentImage != null)
                {
                    // Créer la nouvelle image avec des dimensions fixes
                    var nextImage = new Image
                    {
                        Source = nextImageSource,
                        Aspect = Aspect.AspectFit,
                        Opacity = 0,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        WidthRequest = 250, // Taille fixe
                        HeightRequest = 250 // Taille fixe
                    };

                    imageContainer.Children.Add(nextImage);

                    UpdateProgressBars(_currentIndex);

                    await Task.WhenAll(
                        nextImage.FadeTo(1, 200),
                        currentImage.FadeTo(0, 200)
                    );

                    imageContainer.Children.Remove(currentImage);
                }
            }
        }

        private void UpdateProgressBars(int index)
        {
            var bar1 = this.FindByName<BoxView>("Bar1");
            var bar2 = this.FindByName<BoxView>("Bar2");
            var bar3 = this.FindByName<BoxView>("Bar3");

            if (bar1 != null) bar1.BackgroundColor = Colors.White;
            if (bar2 != null) bar2.BackgroundColor = Colors.White;
            if (bar3 != null) bar3.BackgroundColor = Colors.White;

            switch (index)
            {
                case 0:
                    if (bar1 != null) bar1.BackgroundColor = Color.FromArgb("#FFD602");
                    break;
                case 1:
                    if (bar2 != null) bar2.BackgroundColor = Color.FromArgb("#FFD602");
                    break;
                case 2:
                    if (bar3 != null) bar3.BackgroundColor = Color.FromArgb("#FFD602");
                    break;
            }
        }
    }
}
