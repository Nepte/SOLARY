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
                if (SessionService.IsLoggedIn)
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
        }

        private void InitializeCodeEntries(int count)
        {
            var container = this.FindByName<HorizontalStackLayout>("CodeDigitsContainer");
            if (container == null) return;

            // Nettoyer les entrées existantes
            container.Clear();
            _codeEntries.Clear();

            // Créer les nouvelles entrées avec bordures
            for (int i = 0; i < count; i++)
            {
                var entry = new Entry();

                // Appliquer le style de base
                entry.Style = (Style)Resources["CodeDigitStyle"];

                // Ajouter les gestionnaires d'événements
                entry.TextChanged += OnCodeDigitChanged;
                entry.Focused += OnEntryFocused;
                entry.Unfocused += OnEntryUnfocused;

                // Créer un conteneur avec bordure pour chaque champ
                var entryBorder = new Border
                {
                    Content = entry,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(15) },
                    Stroke = Color.FromArgb("#E0E0E0"),
                    StrokeThickness = 2,
                    BackgroundColor = Colors.White,
                    Shadow = new Shadow
                    {
                        Brush = Color.FromArgb("#20000000"),
                        Offset = new Point(0, 2),
                        Radius = 4
                    }
                };

                _codeEntries.Add(entry);
                container.Add(entryBorder);
            }

            // Mettre à jour le ViewModel
            _viewModel?.UpdateDigitCount(_codeEntries.Count);
        }

        private void OnCodeDigitChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is Entry entry)
            {
                // Limiter à un seul chiffre
                if (e.NewTextValue.Length > 1)
                {
                    entry.Text = e.NewTextValue.Substring(0, 1);
                    return;
                }

                // Passer au champ suivant si un chiffre est entré
                if (!string.IsNullOrEmpty(e.NewTextValue))
                {
                    int currentIndex = _codeEntries.IndexOf(entry);
                    if (currentIndex >= 0 && currentIndex < _codeEntries.Count - 1)
                    {
                        _codeEntries[currentIndex + 1].Focus();
                    }
                }

                // Mettre à jour le code complet
                UpdateCompleteCode();
            }
        }

        private void OnEntryFocused(object? sender, FocusEventArgs e)
        {
            if (sender is Entry entry && e.IsFocused)
            {
                entry.CursorPosition = entry.Text?.Length ?? 0;

                // Effet visuel de focus
                if (entry.Parent is Border border)
                {
                    border.Stroke = Color.FromArgb("#FFD602");
                    border.StrokeThickness = 3;
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
                    border.Stroke = Color.FromArgb("#E0E0E0");
                    border.StrokeThickness = 2;
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
                InitializeCodeEntries(_codeEntries.Count + 1);
            }
        }

        private void OnRemoveDigitClicked(object? sender, EventArgs e)
        {
            if (_codeEntries.Count > 4)
            {
                InitializeCodeEntries(_codeEntries.Count - 1);
            }
        }

        private async void OnSaveCodeClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!SessionService.IsLoggedIn)
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
                foreach (var entry in _codeEntries)
                {
                    entry.Text = "";
                }

                if (_codeEntries.Count > 0)
                {
                    _codeEntries[0].Focus();
                }

                _viewModel?.UpdateCode("");
            }
        }

        private void SetupNavigation()
        {
            var accueilTab = this.FindByName<VerticalStackLayout>("AccueilTab");
            if (accueilTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    // Navigation corrigée vers HomePage au lieu de PopToRootAsync
                    await Navigation.PushAsync(new HomePage());
                };
                accueilTab.GestureRecognizers.Add(tapGesture);
            }

            var statsTab = this.FindByName<VerticalStackLayout>("StatistiquesTab");
            if (statsTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await DisplayAlert("Statistiques", "La page Statistiques n'est pas encore implémentée.", "OK");
                };
                statsTab.GestureRecognizers.Add(tapGesture);
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
                    await DisplayAlert("Paramètres", "La page Paramètres n'est pas encore implémentée.", "OK");
                };
                settingsTab.GestureRecognizers.Add(tapGesture);
            }
        }
    }
}
