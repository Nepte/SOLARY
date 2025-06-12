using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace SOLARY;

[Activity(Theme = "@style/MainTheme", MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    LaunchMode = LaunchMode.SingleTop)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Configuration plein écran immédiate
        ConfigureFullScreen();
    }

    protected override void OnResume()
    {
        base.OnResume();

        // Réappliquer le plein écran au retour
        ConfigureFullScreen();
    }

    private void ConfigureFullScreen()
    {
        if (Window != null)
        {
            // Rendre la barre d'état transparente
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                // Android 11+ - Utiliser WindowInsetsController
                try
                {
                    var insetsController = Window.InsetsController;
                    if (insetsController != null)
                    {
                        insetsController.Hide(WindowInsets.Type.StatusBars());
                        insetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                    }
                    Window.SetDecorFitsSystemWindows(false);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur WindowInsetsController: {ex.Message}");
                    // Fallback vers l'ancienne méthode
                    ConfigureLegacyFullScreen();
                }
            }
            else
            {
                // Android < 11 - Utiliser l'ancienne API
                ConfigureLegacyFullScreen();
            }
        }
    }

    private void ConfigureLegacyFullScreen()
    {
        if (Window?.DecorView != null)
        {
#pragma warning disable CA1422
#pragma warning disable CS0618
            Window.DecorView.SystemUiFlags = (SystemUiFlags)(
                SystemUiFlags.LayoutFullscreen |
                SystemUiFlags.LayoutStable |
                SystemUiFlags.Fullscreen |
                SystemUiFlags.ImmersiveSticky |
                SystemUiFlags.HideNavigation |
                SystemUiFlags.LayoutHideNavigation);
#pragma warning restore CS0618
#pragma warning restore CA1422
        }
    }
}
