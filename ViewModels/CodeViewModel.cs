using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace SOLARY.ViewModels
{
    public class CodeViewModel : INotifyPropertyChanged
    {
        private string _currentCode = "";
        private bool _hasExistingCode = false;
        private int _digitCount = 4;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CurrentCode
        {
            get => _currentCode;
            set
            {
                _currentCode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveCode));
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }

        public bool HasExistingCode
        {
            get => _hasExistingCode;
            set
            {
                _hasExistingCode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(HasCodeColor));
                OnPropertyChanged(nameof(CodeInputTitle));
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }

        public int DigitCount
        {
            get => _digitCount;
            set
            {
                _digitCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddDigit));
                OnPropertyChanged(nameof(CanRemoveDigit));
            }
        }

        public string StatusText => HasExistingCode ? "Code configuré" : "Aucun code configuré";
        public string StatusIcon => HasExistingCode ? "✅" : "❌";
        public Color HasCodeColor => HasExistingCode ? Color.FromArgb("#28A745") : Color.FromArgb("#DC3545");
        public string CodeInputTitle => HasExistingCode ? "Modifier votre code" : "Créer votre code";
        public string SaveButtonText => HasExistingCode ? "Modifier le code" : "Sauvegarder le code";

        public bool CanSaveCode => CurrentCode.Length >= 4 && CurrentCode.Length == DigitCount;
        public bool CanAddDigit => DigitCount < 10;
        public bool CanRemoveDigit => DigitCount > 4;

        public void UpdateCode(string code)
        {
            CurrentCode = code;
        }

        public void SetExistingCode(string code)
        {
            HasExistingCode = !string.IsNullOrEmpty(code);
            if (HasExistingCode)
            {
                CurrentCode = code;
                DigitCount = code.Length;
            }
        }

        public void UpdateDigitCount(int count)
        {
            DigitCount = count;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
