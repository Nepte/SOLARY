using System.Text.RegularExpressions;
using SOLARY.ViewModels;

namespace SOLARY.Views
{
    public partial class RegisterPage : ContentPage
    {


        public RegisterPage()
        {
            InitializeComponent();
            BindingContext = new RegisterViewModel();

        }

        private void OnPhoneNumberTextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;
            if (entry == null) return;

            // Garder uniquement les chiffres
            var newText = Regex.Replace(e.NewTextValue ?? string.Empty, @"[^\d]", "");

            // Limiter la longueur à 10 caractères pour les numéros français
            if (newText.Length > 10)
            {
                newText = newText.Substring(0, 10);
            }

            // Mettre à jour le texte
            entry.Text = newText;
        }

        private void TogglePasswordVisibility(object sender, EventArgs e)
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

        private void ToggleConfirmPasswordVisibility(object sender, EventArgs e)
        {
            if (ConfirmPasswordEntry.IsPassword)
            {
                ConfirmPasswordEntry.IsPassword = false;
                ConfirmEyeIcon.Source = "eye_open.png";
            }
            else
            {
                ConfirmPasswordEntry.IsPassword = true;
                ConfirmEyeIcon.Source = "eye_closed.png";
            }
        }

    }
}
