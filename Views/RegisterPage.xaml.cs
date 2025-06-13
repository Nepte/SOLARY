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

            // Limiter la longueur � 10 caract�res pour les num�ros fran�ais
            if (newText.Length > 10)
            {
                newText = newText.Substring(0, 10);
            }

            // Mettre � jour le texte
            entry.Text = newText;
        }

        // M�thode modifi�e pour synchroniser les deux champs de mot de passe
        private void TogglePasswordVisibility(object sender, EventArgs e)
        {
            // Inverser l'�tat de visibilit� des deux champs de mot de passe
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;

            // Mettre � jour les deux ic�nes
            EyeIcon.Source = PasswordEntry.IsPassword ? "eye_closed.png" : "eye_open.png";
            ConfirmEyeIcon.Source = PasswordEntry.IsPassword ? "eye_closed.png" : "eye_open.png";
        }

        // Cette m�thode n'est plus n�cessaire car nous utilisons une seule m�thode pour les deux champs
        // Mais nous la gardons pour �viter les erreurs si elle est r�f�renc�e ailleurs
        private void ToggleConfirmPasswordVisibility(object sender, EventArgs e)
        {
            // Rediriger vers la m�thode principale pour synchroniser les deux champs
            TogglePasswordVisibility(sender, e);
        }

    }
}
