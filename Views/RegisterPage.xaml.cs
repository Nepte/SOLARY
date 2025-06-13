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

        // Méthode modifiée pour synchroniser les deux champs de mot de passe
        private void TogglePasswordVisibility(object sender, EventArgs e)
        {
            // Inverser l'état de visibilité des deux champs de mot de passe
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;

            // Mettre à jour les deux icônes
            EyeIcon.Source = PasswordEntry.IsPassword ? "eye_closed.png" : "eye_open.png";
            ConfirmEyeIcon.Source = PasswordEntry.IsPassword ? "eye_closed.png" : "eye_open.png";
        }

        // Cette méthode n'est plus nécessaire car nous utilisons une seule méthode pour les deux champs
        // Mais nous la gardons pour éviter les erreurs si elle est référencée ailleurs
        private void ToggleConfirmPasswordVisibility(object sender, EventArgs e)
        {
            // Rediriger vers la méthode principale pour synchroniser les deux champs
            TogglePasswordVisibility(sender, e);
        }

    }
}
