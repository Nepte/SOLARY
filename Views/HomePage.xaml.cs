using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SOLARY.ViewModels;

namespace SOLARY.Views
{
    public partial class HomePage : ContentPage
    {
        private Random random = new Random();
        private System.Timers.Timer? usageUpdateTimer = null;
        private BoxView? progressBar;
        private Label? percentLabel;
        private GraphicsView? circularProgressView;
        private Image? electricityIcon;
        private HomeViewModel? viewModel;

        public HomePage()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            // Essayer de récupérer le ViewModel depuis l'injection de dépendances
            try
            {
                viewModel = Handler?.MauiContext?.Services.GetService<HomeViewModel>();
            }
            catch
            {
                // Fallback si l'injection ne fonctionne pas
                viewModel = null;
            }

            // Si pas de ViewModel injecté, créer une instance par défaut
            if (viewModel == null)
            {
                viewModel = new HomeViewModel();
            }

            BindingContext = viewModel;
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (viewModel == null) return;

            // NE PAS écraser CurrentKwh ici - il sera chargé depuis la BDD
            // viewModel.CurrentKwh = 30.276; // ❌ SUPPRIMÉ - écrasait la valeur de la BDD

            // Initialiser seulement les valeurs qui ne viennent pas de la BDD
            viewModel.TotalEnergy = 36.2;
            viewModel.UsedEnergy = 28.2;
            viewModel.Capacity = 42.0;
            viewModel.PanelGenerated = 140.65;
            viewModel.IsDirectMode = true;

            // Forcer le calcul initial de la largeur de la barre
            UpdateProgressBarWidth(viewModel.BatteryProgress);

            // Trouver les éléments UI
            circularProgressView = this.FindByName<GraphicsView>("CircularProgressView");
            electricityIcon = this.FindByName<Image>("ElectricityIcon");
            percentLabel = this.FindByName<Label>("PercentLabel");

            // Configurer la simulation d'utilisation avec les vraies données BDD
            SetupUsageSimulation();

            // Trouver la barre de progression
            var progressContainer = this.FindByName<Grid>("ProgressContainer");
            if (progressContainer != null)
            {
                progressBar = progressContainer.Children.OfType<BoxView>().ElementAtOrDefault(1);
                UpdateProgressBarWidth(viewModel.BatteryProgress);

                progressContainer.SizeChanged += (s, e) => {
                    UpdateProgressBarWidth(viewModel.BatteryProgress);
                };
            }

            // Mettre à jour les couleurs initiales
            UpdateProgressColors(viewModel.BatteryLevel);

            // Setup navigation
            SetupNavigation();

            // Configurer les gestionnaires d'événements pour le graphique à métrique unique
            SetupSingleMetricChartInteractions();

            // S'abonner aux changements du ViewModel
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(HomeViewModel.BatteryLevel))
                {
                    UpdateProgressColors(viewModel.BatteryLevel);
                    UpdateProgressBarWidth(viewModel.BatteryProgress);
                }
            };
        }

        private void SetupSingleMetricChartInteractions()
        {
            System.Diagnostics.Debug.WriteLine("🎯 Configuration des interactions pour le graphique à métrique unique");

            var singleMetricChart = this.FindByName<GraphicsView>("SingleMetricChart");
            var previousButton = this.FindByName<Button>("PreviousMetricButton");
            var nextButton = this.FindByName<Button>("NextMetricButton");
            var swipeLeft = this.FindByName<SwipeGestureRecognizer>("SwipeLeftGesture");
            var swipeRight = this.FindByName<SwipeGestureRecognizer>("SwipeRightGesture");

            if (singleMetricChart != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnSingleMetricChartTapped;
                singleMetricChart.GestureRecognizers.Add(tapGesture);
                System.Diagnostics.Debug.WriteLine("✅ Interaction tap configurée pour SingleMetricChart");
            }

            if (previousButton != null)
            {
                previousButton.Clicked += (s, e) => {
                    viewModel?.PreviousMetric();
                    System.Diagnostics.Debug.WriteLine("⬅️ Métrique précédente");
                };
            }

            if (nextButton != null)
            {
                nextButton.Clicked += (s, e) => {
                    viewModel?.NextMetric();
                    System.Diagnostics.Debug.WriteLine("➡️ Métrique suivante");
                };
            }

            if (swipeLeft != null)
            {
                swipeLeft.Swiped += (s, e) => {
                    viewModel?.NextMetric();
                    System.Diagnostics.Debug.WriteLine("👈 Swipe gauche - Métrique suivante");
                };
            }

            if (swipeRight != null)
            {
                swipeRight.Swiped += (s, e) => {
                    viewModel?.PreviousMetric();
                    System.Diagnostics.Debug.WriteLine("👉 Swipe droite - Métrique précédente");
                };
            }
        }

        private void OnSingleMetricChartTapped(object? sender, TappedEventArgs e)
        {
            if (sender is GraphicsView graphView && viewModel != null)
            {
                var position = e.GetPosition(graphView);
                if (!position.HasValue) return;

                System.Diagnostics.Debug.WriteLine($"📊 Tap détecté sur le graphique à la position ({position.Value.X:F1}, {position.Value.Y:F1})");

                if (viewModel.SingleMetricChartDrawable is SingleMetricChartDrawable chartDrawable)
                {
                    var bounds = new RectF(0, 0, (float)graphView.Width, (float)graphView.Height);
                    var pointInfo = chartDrawable.GetPointAt((float)position.Value.X, (float)position.Value.Y, bounds);

                    if (pointInfo.index >= 0)
                    {
                        // Mettre à jour le point sélectionné
                        chartDrawable.SetSelectedPoint(pointInfo.index);
                        graphView.Invalidate();

                        string metricName = viewModel.CurrentMetricDisplayName;
                        System.Diagnostics.Debug.WriteLine($"🎯 Point sélectionné - {metricName}: {pointInfo.value:F1} à {pointInfo.time:HH:mm}");
                    }
                }
            }
        }

        private void UpdateProgressBarWidth(double progress)
        {
            if (viewModel != null)
            {
                // Calculer la largeur basée sur le conteneur (335px = 375px card - 40px padding)
                double containerWidth = 335.0;
                viewModel.ProgressBarWidth = Math.Max(0, containerWidth * progress);

                // Forcer la mise à jour de l'UI
                Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                {
                    OnPropertyChanged(nameof(viewModel.ProgressBarWidth));
                });
            }
        }

        private void SetupUsageSimulation()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Configuration du timer de mise à jour des données BDD");

            // Utiliser les vraies données BDD au lieu de simulation
            usageUpdateTimer = new System.Timers.Timer(10000); // 10 secondes
            usageUpdateTimer.Elapsed += async (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("⏰ Timer déclenché - Rechargement des données BDD");

                // Recharger les données depuis la BDD
                if (viewModel != null)
                {
                    await viewModel.LoadLatestMeasureData();

                    Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                    {
                        // Mettre à jour l'UI avec les vraies données
                        UpdateProgressColors(viewModel.BatteryLevel);
                        UpdateProgressBarWidth(viewModel.BatteryProgress);

                        if (circularProgressView != null)
                        {
                            circularProgressView.Invalidate();
                        }

                        // Invalider le graphique pour le mettre à jour
                        RefreshSingleMetricChart();
                    });
                }
            };
            usageUpdateTimer.Start();
            System.Diagnostics.Debug.WriteLine("✅ Timer de mise à jour démarré (10s)");
        }

        private void RefreshSingleMetricChart()
        {
            // Invalider le graphique à métrique unique pour le mettre à jour avec les nouvelles données
            var singleMetricChart = this.FindByName<GraphicsView>("SingleMetricChart");
            singleMetricChart?.Invalidate();

            System.Diagnostics.Debug.WriteLine("🔄 Graphique à métrique unique actualisé");
        }

        private void UpdateProgressColors(int batteryLevel)
        {
            bool isLowBattery = batteryLevel < 30;
            Color progressColor = isLowBattery ? Colors.Red : Color.FromArgb("#FFD602");

            if (progressBar != null)
            {
                progressBar.Color = progressColor;
            }

            if (percentLabel != null)
            {
                percentLabel.TextColor = progressColor;
            }

            if (circularProgressView != null && circularProgressView.Drawable is CircularProgressDrawable circularDrawable)
            {
                circularDrawable.ProgressColor = progressColor;
                circularProgressView.Invalidate();
            }

            if (electricityIcon != null)
            {
                electricityIcon.Source = isLowBattery ? "icon_electricity_red.png" : "icon_electricity_yellow.png";
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            System.Diagnostics.Debug.WriteLine("👋 HomePage disparition - Nettoyage des ressources");

            usageUpdateTimer?.Stop();
            usageUpdateTimer?.Dispose();
            viewModel?.Dispose();
        }

        private void SetupNavigation()
        {
            var locateTab = this.FindByName<VerticalStackLayout>("LocaliserTab");
            if (locateTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PushAsync(new MapPage());
                };
                locateTab.GestureRecognizers.Add(tapGesture);
            }

            var codeTab = this.FindByName<VerticalStackLayout>("CodeTab");
            if (codeTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PushAsync(new CodePage());
                };
                codeTab.GestureRecognizers.Add(tapGesture);
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

    // Classes pour le dessin (conservées pour compatibilité)
    public class CircularProgressDrawable : IDrawable
    {
        public double Progress { get; set; }
        public Color ProgressColor { get; set; } = Color.FromArgb("#FFD602");
        public Color BackgroundColor { get; set; } = Color.FromArgb("#E5E5E5");

        public CircularProgressDrawable(double progress)
        {
            Progress = Math.Clamp(progress, 0, 1);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float width = dirtyRect.Width;
            float height = dirtyRect.Height;
            float size = Math.Min(width, height);

            float centerX = width / 2f;
            float centerY = height / 2f;
            float radius = (size / 2f) - 3f;
            float strokeWidth = 2.5f;

            if (radius <= 0) return;

            canvas.StrokeSize = strokeWidth;
            canvas.StrokeDashPattern = null;
            canvas.StrokeColor = BackgroundColor;
            canvas.DrawCircle(centerX, centerY, radius);
        }
    }
}
