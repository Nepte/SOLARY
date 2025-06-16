using Microsoft.Maui.Controls;
using SOLARY.ViewModels;
using SOLARY.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace SOLARY.Views
{
    public partial class CodePage : ContentPage
    {
        private CodeViewModel? _viewModel;
        private List<Entry> _codeEntries = new List<Entry>();
        private readonly IUserService _userService;
        private bool _isInitializing = false;
        private int _currentFocusedIndex = -1;

        public CodePage()
        {
            InitializeComponent();

            _userService = new UserService();
            _viewModel = new CodeViewModel();
            BindingContext = _viewModel;

            // Configurer la navigation
            SetupNavigation();

            // Initialiser avec 4 chiffres par défaut
            InitializeCodeEntries(4);

            // Charger le code existant
            LoadExistingCode();
        }

        private async void LoadExistingCode()
        {
            try
            {
                _isInitializing = true;

                if (SessionService.IsLoggedIn && SessionService.CurrentUserId.HasValue)
                {
                    int userId = SessionService.CurrentUserId.Value;
                    var user = await _userService.GetUserById(userId);

                    if (user != null && !string.IsNullOrEmpty(user.CodeCasiers))
                    {
                        _viewModel?.SetExistingCode(user.CodeCasiers);

                        // Ajuster le nombre d'entrées selon la longueur du code existant
                        int codeLength = user.CodeCasiers.Length;
                        if (codeLength != _codeEntries.Count)
                        {
                            InitializeCodeEntries(codeLength);
                        }

                        // Remplir les entrées avec le code existant
                        for (int i = 0; i < user.CodeCasiers.Length && i < _codeEntries.Count; i++)
                        {
                            _codeEntries[i].Text = user.CodeCasiers[i].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du chargement du code existant: {ex.Message}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void InitializeCodeEntries(int count)
        {
            var container = this.FindByName<HorizontalStackLayout>("CodeDigitsContainer");
            if (container == null) return;

            _isInitializing = true;

            // Nettoyer les entrées existantes
            container.Clear();
            _codeEntries.Clear();

            // Calculer les dimensions adaptatives selon le nombre de chiffres pour qu'ils restent sur une ligne
            var dimensions = GetAdaptiveDimensions(count);

            // Créer les nouvelles entrées avec bordures
            for (int i = 0; i < count; i++)
            {
                var entry = new Entry
                {
                    FontSize = dimensions.FontSize,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#333333"),
                    BackgroundColor = Colors.Transparent,
                    HeightRequest = dimensions.Size,
                    WidthRequest = dimensions.Size,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    MaxLength = 1,
                    Keyboard = Keyboard.Numeric,
                    IsEnabled = true
                };

                // Ajouter les gestionnaires d'événements
                entry.TextChanged += OnCodeDigitChanged;
                entry.Focused += OnEntryFocused;
                entry.Unfocused += OnEntryUnfocused;
                entry.Completed += OnEntryCompleted;

                // Créer un conteneur avec bordure pour chaque champ - Design amélioré
                var entryBorder = new Border
                {
                    Content = entry,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) }, // Plus arrondi
                    Stroke = Color.FromArgb("#E8E9EA"), // Couleur plus douce
                    StrokeThickness = 1, // Plus fin
                    BackgroundColor = Colors.White,
                    Margin = new Thickness(dimensions.Margin),
                    HeightRequest = dimensions.Size,
                    WidthRequest = dimensions.Size,
                    Shadow = new Shadow
                    {
                        Brush = Color.FromArgb("#15000000"), // Ombre plus subtile
                        Offset = new Point(0, 2f),
                        Radius = 8f
                    }
                };

                // Ajouter un gestionnaire de tap pour permettre la sélection directe
                var tapGesture = new TapGestureRecognizer();
                int index = i; // Capturer l'index pour la closure
                tapGesture.Tapped += (s, e) => {
                    entry.Focus();
                    _currentFocusedIndex = index;
                };
                entryBorder.GestureRecognizers.Add(tapGesture);

                _codeEntries.Add(entry);
                container.Add(entryBorder);
            }

            // Mettre à jour le ViewModel
            _viewModel?.UpdateDigitCount(_codeEntries.Count);

            _isInitializing = false;
        }

        private (double Size, double FontSize, double Margin) GetAdaptiveDimensions(int digitCount)
        {
            // Calculer la largeur disponible (largeur écran - marges)
            var screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
            var availableWidth = Math.Min(400, screenWidth - 80); // Largeur max 400, marges 40 de chaque côté

            // Calculer la taille optimale pour que tout tienne sur une ligne
            var totalMargin = (digitCount - 1) * 8; // 4px de marge de chaque côté
            var availableForBoxes = availableWidth - totalMargin;
            var optimalSize = availableForBoxes / digitCount;

            // Définir des tailles min/max pour garder un bon aspect
            var size = Math.Max(45, Math.Min(65, optimalSize));

            // Ajuster la police selon la taille
            var fontSize = size switch
            {
                >= 60 => 24,
                >= 55 => 22,
                >= 50 => 20,
                _ => 18
            };

            return (size, fontSize, 4);
        }

        private void OnCodeDigitChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry && !_isInitializing)
            {
                int currentIndex = _codeEntries.IndexOf(entry);

                // Gérer la suppression (backspace)
                if (string.IsNullOrEmpty(e.NewTextValue) && !string.IsNullOrEmpty(e.OldTextValue))
                {
                    // Si on supprime et qu'on n'est pas sur le premier champ, aller au précédent
                    if (currentIndex > 0)
                    {
                        _codeEntries[currentIndex - 1].Focus();
                        _currentFocusedIndex = currentIndex - 1;
                    }
                    UpdateCompleteCode();
                    return;
                }

                // Gérer l'ajout de caractères
                if (!string.IsNullOrEmpty(e.NewTextValue))
                {
                    // Si on tape plusieurs chiffres d'un coup (collage ou saisie rapide)
                    if (e.NewTextValue.Length > 1)
                    {
                        HandleMultipleDigitInput(e.NewTextValue, currentIndex);
                        return;
                    }

                    // Limiter à un seul chiffre numérique
                    if (e.NewTextValue.Length == 1 && char.IsDigit(e.NewTextValue[0]))
                    {
                        // Passer au champ suivant si un chiffre est entré
                        if (currentIndex >= 0 && currentIndex < _codeEntries.Count - 1)
                        {
                            _codeEntries[currentIndex + 1].Focus();
                            _currentFocusedIndex = currentIndex + 1;
                        }
                    }
                    else
                    {
                        // Rejeter les caractères non numériques
                        entry.Text = e.OldTextValue;
                        return;
                    }
                }

                // Mettre à jour le code complet
                UpdateCompleteCode();
            }
        }

        private void HandleMultipleDigitInput(string input, int startIndex)
        {
            // Nettoyer l'input pour ne garder que les chiffres
            string digits = "";
            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    digits += c;
                }
            }

            // Remplir les champs à partir de l'index actuel
            for (int i = 0; i < digits.Length && (startIndex + i) < _codeEntries.Count; i++)
            {
                _codeEntries[startIndex + i].Text = digits[i].ToString();
            }

            // Positionner le focus sur le prochain champ libre ou le dernier
            int nextIndex = Math.Min(startIndex + digits.Length, _codeEntries.Count - 1);
            _codeEntries[nextIndex].Focus();
            _currentFocusedIndex = nextIndex;

            UpdateCompleteCode();
        }

        private void OnEntryFocused(object? sender, FocusEventArgs e)
        {
            if (sender is Entry entry && e.IsFocused)
            {
                _currentFocusedIndex = _codeEntries.IndexOf(entry);

                // Sélectionner tout le texte pour faciliter la modification
                entry.CursorPosition = 0;
                entry.SelectionLength = entry.Text?.Length ?? 0;

                // Effet visuel de focus amélioré - style plus moderne
                if (entry.Parent is Border border)
                {
                    border.Stroke = Color.FromArgb("#FFD602");
                    border.StrokeThickness = 2;
                    // Ombre plus prononcée avec couleur dorée
                    border.Shadow = new Shadow
                    {
                        Brush = Color.FromArgb("#30FFD602"),
                        Offset = new Point(0, 3f),
                        Radius = 12f
                    };
                    // Légère élévation visuelle
                    border.Scale = 1.05;
                }
            }
        }

        private void OnEntryUnfocused(object? sender, FocusEventArgs e)
        {
            if (sender is Entry entry && !e.IsFocused)
            {
                // Retirer l'effet visuel de focus
                if (entry.Parent is Border border)
                {
                    border.Stroke = Color.FromArgb("#E8E9EA");
                    border.StrokeThickness = 1;
                    // Restaurer l'ombre normale
                    border.Shadow = new Shadow
                    {
                        Brush = Color.FromArgb("#15000000"),
                        Offset = new Point(0, 2f),
                        Radius = 8f
                    };
                    // Restaurer la taille normale
                    border.Scale = 1.0;
                }
            }
        }

        private void OnEntryCompleted(object? sender, EventArgs e)
        {
            // Gérer la touche Entrée - passer au champ suivant ou terminer
            if (_currentFocusedIndex >= 0 && _currentFocusedIndex < _codeEntries.Count - 1)
            {
                _codeEntries[_currentFocusedIndex + 1].Focus();
            }
            else
            {
                // Retirer le focus du dernier champ
                if (sender is Entry entry)
                {
                    entry.Unfocus();
                }
            }
        }

        private void UpdateCompleteCode()
        {
            string completeCode = "";
            foreach (var entry in _codeEntries)
            {
                completeCode += entry.Text ?? "";
            }

            _viewModel?.UpdateCode(completeCode);
        }

        private void OnAddDigitClicked(object? sender, EventArgs e)
        {
            if (_codeEntries.Count < 10)
            {
                // Sauvegarder le code actuel
                string currentCode = "";
                foreach (var entry in _codeEntries)
                {
                    currentCode += entry.Text ?? "";
                }

                // IMPORTANT: Retirer le focus de tous les champs AVANT de recréer
                foreach (var entry in _codeEntries)
                {
                    entry.Unfocus();
                }

                // Attendre un peu pour que le focus soit complètement retiré
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(50); // Petit délai pour éviter le focus automatique

                    InitializeCodeEntries(_codeEntries.Count + 1);

                    // Restaurer le code
                    for (int i = 0; i < currentCode.Length && i < _codeEntries.Count; i++)
                    {
                        _codeEntries[i].Text = currentCode[i].ToString();
                    }

                    UpdateCompleteCode();
                });
            }
        }

        private void OnRemoveDigitClicked(object? sender, EventArgs e)
        {
            if (_codeEntries.Count > 4)
            {
                // Sauvegarder le code actuel
                string currentCode = "";
                foreach (var entry in _codeEntries)
                {
                    currentCode += entry.Text ?? "";
                }

                // IMPORTANT: Retirer le focus de tous les champs AVANT de recréer
                foreach (var entry in _codeEntries)
                {
                    entry.Unfocus();
                }

                // Attendre un peu pour que le focus soit complètement retiré
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(50); // Petit délai pour éviter le focus automatique

                    InitializeCodeEntries(_codeEntries.Count - 1);

                    // Restaurer le code (tronqué)
                    int maxLength = Math.Min(currentCode.Length, _codeEntries.Count);
                    for (int i = 0; i < maxLength; i++)
                    {
                        _codeEntries[i].Text = currentCode[i].ToString();
                    }

                    UpdateCompleteCode();
                });
            }
        }

        private async void OnSaveCodeClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!SessionService.IsLoggedIn || !SessionService.CurrentUserId.HasValue)
                {
                    await DisplayAlert("Erreur", "Vous devez être connecté pour sauvegarder votre code.", "OK");
                    return;
                }

                string code = "";
                foreach (var entry in _codeEntries)
                {
                    code += entry.Text ?? "";
                }

                if (code.Length < 4)
                {
                    await DisplayAlert("Code invalide", "Votre code doit contenir au moins 4 chiffres.", "OK");
                    return;
                }

                if (code.Length != _codeEntries.Count)
                {
                    await DisplayAlert("Code incomplet", "Veuillez remplir tous les champs du code.", "OK");
                    return;
                }

                int userId = SessionService.CurrentUserId.Value;
                bool success = await _userService.UpdateLockerCode(userId, code);

                if (success)
                {
                    await DisplayAlert("Succès", "Votre code personnel a été sauvegardé avec succès !", "OK");
                    _viewModel?.SetExistingCode(code);
                }
                else
                {
                    await DisplayAlert("Erreur", "Impossible de sauvegarder votre code. Veuillez réessayer.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Une erreur s'est produite : {ex.Message}", "OK");
            }
        }

        private async void OnClearCodeClicked(object? sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Effacer le code",
                "Êtes-vous sûr de vouloir effacer votre code ?",
                "Oui",
                "Annuler");

            if (confirm)
            {
                _isInitializing = true;

                foreach (var entry in _codeEntries)
                {
                    entry.Unfocus();
                    entry.Text = "";
                }

                _isInitializing = false;
                _viewModel?.UpdateCode("");

                // Ne pas donner le focus automatiquement après effacement
                _currentFocusedIndex = -1;
            }
        }

        private void SetupNavigation()
        {
            var accueilTab = this.FindByName<VerticalStackLayout>("AccueilTab");
            if (accueilTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PushAsync(new HomePage());
                };
                accueilTab.GestureRecognizers.Add(tapGesture);
            }

            var locateTab = this.FindByName<VerticalStackLayout>("LocaliserTab");
            if (locateTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PushAsync(new MapPage());
                };
                locateTab.GestureRecognizers.Add(tapGesture);
            }

            var settingsTab = this.FindByName<VerticalStackLayout>("ParametresTab");
            if (settingsTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PushAsync(new SettingsPage());
                };
                settingsTab.GestureRecognizers.Add(tapGesture);
            }
        }
    }
}
