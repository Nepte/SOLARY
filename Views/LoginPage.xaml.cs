using SOLARY.ViewModels;

namespace SOLARY.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
        BindingContext = new LoginViewModel(); // Définissez le ViewModel ici

    }

    private void TogglePasswordVisibility(object sender, EventArgs e)
    {
        if (PasswordEntry.IsPassword)
        {
            PasswordEntry.IsPassword = false;
            EyeIcon.Source = "eye_open.png"; // Icône de l'œil ouvert
        }
        else
        {
            PasswordEntry.IsPassword = true;
            EyeIcon.Source = "eye_closed.png"; // Icône de l'œil fermé
        }
    }

}