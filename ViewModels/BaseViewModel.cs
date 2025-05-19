using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SOLARY.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged; // L'événement est nullable

        /// <summary>
        /// Déclenche l'événement PropertyChanged pour notifier les changements de propriété.
        /// </summary>
        /// <param name="propertyName">Le nom de la propriété modifiée.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (!string.IsNullOrEmpty(propertyName)) // Vérification pour éviter les erreurs nulles
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
