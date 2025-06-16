using SOLARY.ViewModels;

namespace SOLARY.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = new SettingsViewModel();
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
}
