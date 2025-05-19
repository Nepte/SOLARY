using SOLARY.ViewModels;
using Microsoft.Maui.Controls;

namespace SOLARY.Views
{
    public partial class OtpPage : ContentPage
    {
        private bool _isHandlingTextChange;

        public OtpPage(string email)
        {
            InitializeComponent();
            BindingContext = new OtpViewModel(email);
        }

        // ===== CASE 1 =====
        private void Entry1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isHandlingTextChange) return;
            _isHandlingTextChange = true;

            // Tape un caractère => aller case suivante
            if (e.NewTextValue.Length == 1)
            {
                Entry2.Focus();
            }
            // Efface un caractère => (il n’y a pas de case précédente pour la 1)
            // Efface sur une case déjà vide => rien à faire non plus

            _isHandlingTextChange = false;
        }

        // ===== CASE 2 =====
        private void Entry2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isHandlingTextChange) return;
            _isHandlingTextChange = true;

            if (e.NewTextValue.Length == 1)
            {
                // Tape un caractère => focus case suivante
                Entry3.Focus();
            }
            else if ( // Effacement d’un caractère => old=1 => new=0
                e.OldTextValue?.Length == 1 && e.NewTextValue.Length == 0)
            {
                Entry1.Focus();
            }
            else if ( // Effacement sur une case déjà vide => old=0 => new=0
                string.IsNullOrEmpty(e.OldTextValue) && string.IsNullOrEmpty(e.NewTextValue))
            {
                Entry1.Focus();
            }

            _isHandlingTextChange = false;
        }

        // ===== CASE 3 =====
        private void Entry3_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isHandlingTextChange) return;
            _isHandlingTextChange = true;

            if (e.NewTextValue.Length == 1)
            {
                Entry4.Focus();
            }
            else if (e.OldTextValue?.Length == 1 && e.NewTextValue.Length == 0)
            {
                Entry2.Focus();
            }
            else if (string.IsNullOrEmpty(e.OldTextValue) && string.IsNullOrEmpty(e.NewTextValue))
            {
                Entry2.Focus();
            }

            _isHandlingTextChange = false;
        }

        // ===== CASE 4 =====
        private void Entry4_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isHandlingTextChange) return;
            _isHandlingTextChange = true;

            if (e.NewTextValue.Length == 1)
            {
                Entry5.Focus();
            }
            else if (e.OldTextValue?.Length == 1 && e.NewTextValue.Length == 0)
            {
                Entry3.Focus();
            }
            else if (string.IsNullOrEmpty(e.OldTextValue) && string.IsNullOrEmpty(e.NewTextValue))
            {
                Entry3.Focus();
            }

            _isHandlingTextChange = false;
        }

        // ===== CASE 5 =====
        private void Entry5_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isHandlingTextChange) return;
            _isHandlingTextChange = true;

            if (e.NewTextValue.Length == 1)
            {
                Entry6.Focus();
            }
            else if (e.OldTextValue?.Length == 1 && e.NewTextValue.Length == 0)
            {
                Entry4.Focus();
            }
            else if (string.IsNullOrEmpty(e.OldTextValue) && string.IsNullOrEmpty(e.NewTextValue))
            {
                Entry4.Focus();
            }

            _isHandlingTextChange = false;
        }

        // ===== CASE 6 =====
        private void Entry6_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isHandlingTextChange) return;
            _isHandlingTextChange = true;

            if (e.NewTextValue.Length == 1)
            {
                // Optionnel : auto‐valider quand on a tapé le 6e chiffre
                // (BindingContext as OtpViewModel)?.ValidateOtpCommand?.Execute(null);
            }
            else if (e.OldTextValue?.Length == 1 && e.NewTextValue.Length == 0)
            {
                Entry5.Focus();
            }
            else if (string.IsNullOrEmpty(e.OldTextValue) && string.IsNullOrEmpty(e.NewTextValue))
            {
                Entry5.Focus();
            }

            _isHandlingTextChange = false;
        }
    }
}
