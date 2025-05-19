using SOLARY.ViewModels;

namespace SOLARY.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
        BindingContext = new LoginViewModel(); // D�finissez le ViewModel ici

    }

    private void TogglePasswordVisibility(object sender, EventArgs e)
    {
        if (PasswordEntry.IsPassword)
        {
            PasswordEntry.IsPassword = false;
            EyeIcon.Source = "eye_open.png"; // Ic�ne de l'�il ouvert
        }
        else
        {
            PasswordEntry.IsPassword = true;
            EyeIcon.Source = "eye_closed.png"; // Ic�ne de l'�il ferm�
        }
    }

}