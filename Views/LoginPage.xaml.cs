using SOLARY.ViewModels;

namespace SOLARY.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = new LoginViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ConfigureStatusBar();
    }

    private void ConfigureStatusBar()
    {
        try
        {
            // Pour iOS - configuration supplémentaire si nécessaire
            if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
#if IOS
                // La configuration est déjà dans Info.plist, mais on peut forcer ici si nécessaire
                var viewController = Microsoft.Maui.ApplicationModel.Platform.GetCurrentUIViewController();
                if (viewController != null)
                {
                    viewController.SetNeedsStatusBarAppearanceUpdate();
                }
#endif
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration de la barre de statut: {ex.Message}");
        }
    }

    private void TogglePasswordVisibility(object? sender, EventArgs e)
    {
        if (PasswordEntry.IsPassword)
        {
            PasswordEntry.IsPassword = false;
            EyeIcon.Source = "eye_open.png";
        }
        else
        {
            PasswordEntry.IsPassword = true;
            EyeIcon.Source = "eye_closed.png";
        }
    }
}
