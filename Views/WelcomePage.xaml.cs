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

        public ICommand NavigateToLoginCommand { get; }

        public WelcomePage()
        {
            InitializeComponent();

            // Masquer la barre de statut violette
            MakeStatusBarTransparent();

            // Initialiser la commande
            NavigateToLoginCommand = new Command(async () => await NavigateToLoginPage());
            BindingContext = this; // Définir le contexte de binding

            // Adapter les tailles en fonction de l'écran
            AdaptToScreenSize();

            SetupImageContainer();
            StartImageRotationAsync();
        }

        private void MakeStatusBarTransparent()
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
                        
                        // Vérifier la version d'Android
                        if (OperatingSystem.IsAndroidVersionAtLeast(30)) // Android 11 (API 30) ou supérieur
                        {
                            // Utiliser la réflexion pour accéder à WindowInsetsController
                            ConfigureModernStatusBarWithReflection(activity);
                        }
                        else
                        {
                            // Pour les versions antérieures à Android 11, utiliser l'ancienne API
                            ConfigureLegacyStatusBar(activity);
                        }
                    }
#endif
                }
                // Pour iOS, la configuration est dans Info.plist
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration de la barre de statut: {ex.Message}");
            }
        }

#if ANDROID
        private static void ConfigureModernStatusBarWithReflection(Android.App.Activity activity)
        {
            try
            {
                var window = activity.Window;
                if (window != null)
                {
                    // Utiliser la réflexion pour accéder à WindowInsetsController
                    var insetsControllerProperty = window.GetType().GetProperty("InsetsController");
                    if (insetsControllerProperty != null)
                    {
                        var insetsController = insetsControllerProperty.GetValue(window);
                        if (insetsController != null)
                        {
                            // Obtenir les types nécessaires par réflexion
                            var insetsControllerType = insetsController.GetType();
                            
                            // Méthode Hide - masquer la barre d'état
                            var hideMethod = insetsControllerType.GetMethod("Hide");
                            if (hideMethod != null)
                            {
                                // Obtenir la valeur pour StatusBars (généralement 1)
                                var windowInsetsType = Type.GetType("Android.Views.WindowInsets+Type, Mono.Android");
                                if (windowInsetsType != null)
                                {
                                    var statusBarsField = windowInsetsType.GetMethod("StatusBars");
                                    if (statusBarsField != null)
                                    {
                                        var statusBarsValue = statusBarsField.Invoke(null, null);
                                        hideMethod.Invoke(insetsController, new object[] { statusBarsValue });
                                    }
                                    else
                                    {
                                        // Fallback avec la valeur numérique
                                        hideMethod.Invoke(insetsController, new object[] { 1 });
                                    }
                                }
                            }
                            
                            // Définir le comportement
                            var setBehaviorProperty = insetsControllerType.GetProperty("SystemBarsBehavior");
                            if (setBehaviorProperty != null)
                            {
                                // Valeur 1 correspond à BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                                setBehaviorProperty.SetValue(insetsController, 1);
                            }
                        }
                    }
                    
                    // SetDecorFitsSystemWindows avec réflexion
                    var setDecorFitsMethod = window.GetType().GetMethod("SetDecorFitsSystemWindows");
                    if (setDecorFitsMethod != null)
                    {
                        setDecorFitsMethod.Invoke(window, new object[] { false });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur avec la réflexion WindowInsetsController: {ex.Message}");
                // Fallback vers l'ancienne API en cas d'erreur
                ConfigureLegacyStatusBar(activity);
            }
        }

        private static void ConfigureLegacyStatusBar(Android.App.Activity activity)
        {
            try
            {
                var window = activity.Window;
                if (window?.DecorView != null)
                {
                    // Pour toutes les versions, utiliser l'ancienne API avec suppression d'avertissement
#pragma warning disable CA1422 // Validate platform compatibility
#pragma warning disable CS0618 // Type or member is obsolete
                    window.DecorView.SystemUiFlags = (Android.Views.SystemUiFlags)(
                        Android.Views.SystemUiFlags.LayoutFullscreen | 
                        Android.Views.SystemUiFlags.LayoutStable |
                        Android.Views.SystemUiFlags.Fullscreen |
                        Android.Views.SystemUiFlags.ImmersiveSticky);
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA1422 // Validate platform compatibility
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
                    var maxSize = Math.Min(350, screenWidth * 0.8);
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
            await Navigation.PushAsync(new LoginPage());
        }

        private void SetupImageContainer()
        {
            var imageContainer = this.FindByName<Grid>("ImageContainer");
            if (imageContainer != null)
            {
                imageContainer.Children.Add(new Image
                {
                    Source = _images[_currentIndex],
                    Aspect = Aspect.AspectFit,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                });

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
                    var nextImage = new Image
                    {
                        Source = nextImageSource,
                        Aspect = Aspect.AspectFit,
                        Opacity = 0,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
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
