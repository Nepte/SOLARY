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
        private int currentDataPointIndex = 5;
        private float tooltipXPosition;
        private bool isPointerTracking = false;
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

            // Initialiser les valeurs par défaut
            viewModel.CurrentKwh = 30.276;
            viewModel.BatteryLevel = 82; // Valeur réelle de la BDD (pas de simulation)
            viewModel.Voltage = 5.0;     // Valeur constante borne_id = 1
            viewModel.Current = 1.4;     // Valeur variable borne_id = 1  
            viewModel.Power = 7.0;       // Valeur variable borne_id = 1
            viewModel.TotalEnergy = 36.2;
            viewModel.UsedEnergy = 28.2;
            viewModel.Capacity = 42.0;
            viewModel.Co2Reduction = 28.2;
            viewModel.PanelGenerated = 140.65;
            viewModel.IsDirectMode = true;

            // Trouver les éléments UI
            circularProgressView = this.FindByName<GraphicsView>("CircularProgressView");
            electricityIcon = this.FindByName<Image>("ElectricityIcon");
            percentLabel = this.FindByName<Label>("PercentLabel");

            // Initialiser le graphique
            InitializeGraph();

            // Configurer la simulation d'utilisation
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

            // Ajouter des gestionnaires d'événements pour le graphique
            var graphView = this.FindByName<GraphicsView>("GraphView");
            if (graphView != null)
            {
                graphView.StartInteraction += OnGraphStartInteraction;
                graphView.DragInteraction += OnGraphDragInteraction;
                graphView.EndInteraction += OnGraphEndInteraction;
            }

            // Setup navigation
            SetupNavigation();

            // S'abonner aux changements du ViewModel
            viewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(HomeViewModel.BatteryLevel))
                {
                    UpdateProgressColors(viewModel.BatteryLevel);
                    UpdateProgressBarWidth(viewModel.BatteryProgress);
                }
            };
        }

        private void UpdateProgressBarWidth(double progress)
        {
            var progressContainer = this.FindByName<Grid>("ProgressContainer");
            if (progressContainer != null && progressBar != null)
            {
                double containerWidth = progressContainer.Width;
                if (containerWidth > 0)
                {
                    progressBar.WidthRequest = containerWidth * progress;
                }
            }
        }

        private void SetupUsageSimulation()
        {
            // SUPPRIMER la simulation aléatoire - utiliser les vraies données BDD
            usageUpdateTimer = new System.Timers.Timer(10000); // 10 secondes
            usageUpdateTimer.Elapsed += async (s, e) =>
            {
                // Recharger les données depuis la BDD au lieu de simuler
                if (viewModel != null)
                {
                    await viewModel.LoadLatestMeasureData(); // Méthode à rendre publique

                    Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() =>
                    {
                        // Mettre à jour l'UI avec les vraies données
                        UpdateProgressColors(viewModel.BatteryLevel);
                        UpdateProgressBarWidth(viewModel.BatteryProgress);

                        if (circularProgressView != null)
                        {
                            circularProgressView.Invalidate();
                        }
                    });
                }
            };
            usageUpdateTimer.Start();
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

            var statsTab = this.FindByName<VerticalStackLayout>("StatistiquesTab");
            if (statsTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await DisplayAlert("Statistiques", "La page Statistiques n'est pas encore implémentée.", "OK");
                };
                statsTab.GestureRecognizers.Add(tapGesture);
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
                    await DisplayAlert("Paramètres", "La page Paramètres n'est pas encore implémentée.", "OK");
                };
                settingsTab.GestureRecognizers.Add(tapGesture);
            }
        }

        private void InitializeGraph()
        {
            if (viewModel == null) return;

            viewModel.GraphDrawable = new EnhancedGraphDrawable();

            if (circularProgressView != null)
            {
                var circularDrawable = new CircularProgressDrawable(viewModel.BatteryProgress);
                bool isLowBattery = viewModel.BatteryLevel < 30;
                circularDrawable.ProgressColor = isLowBattery ? Colors.Red : Color.FromArgb("#FFD602");
                circularProgressView.Drawable = circularDrawable;
                circularProgressView.Invalidate();
            }

            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () =>
            {
                PositionTooltipAndLine(currentDataPointIndex);
            });
        }

        private void PositionTooltipAndLine(int dataPointIndex)
        {
            var graphView = this.FindByName<GraphicsView>("GraphView");
            if (graphView == null) return;

            var graphWidth = graphView.Width;
            if (graphWidth <= 0) return;

            float pointSpacing = (float)(graphWidth / (EnhancedGraphDrawable.DataPoints.Length - 1));
            tooltipXPosition = dataPointIndex * pointSpacing;

            if (viewModel?.GraphDrawable is EnhancedGraphDrawable graphDrawable)
            {
                graphDrawable.CurrentTooltipIndex = dataPointIndex;
                graphView.Invalidate();
            }

            var tooltipFrame = this.FindByName<Border>("TooltipFrame");
            if (tooltipFrame != null)
            {
                tooltipFrame.TranslationX = tooltipXPosition - (tooltipFrame.Width / 2);
            }

            UpdateTooltip(dataPointIndex);
        }

        private void UpdateTooltip(int dataPointIndex)
        {
            if (dataPointIndex < 0 || dataPointIndex >= EnhancedGraphDrawable.DataPoints.Length)
                return;

            string[] dates = { "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00", "17:30", "18:00", "18:30", "19:00", "19:30" };
            string date = dataPointIndex < dates.Length ? dates[dataPointIndex] : "Feb, 22";

            int value = (int)(EnhancedGraphDrawable.DataPoints[dataPointIndex] * 200);

            var tooltipDate = this.FindByName<Label>("TooltipDate");
            var tooltipValue = this.FindByName<Label>("TooltipValue");

            if (tooltipDate != null)
                tooltipDate.Text = date;

            if (tooltipValue != null)
                tooltipValue.Text = $"{value}kwh";
        }

        private void OnGraphPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    var graphView = this.FindByName<GraphicsView>("GraphView");
                    if (graphView == null) return;

                    var graphWidth = graphView.Width;
                    if (graphWidth <= 0) return;

                    float pointSpacing = (float)(graphWidth / (EnhancedGraphDrawable.DataPoints.Length - 1));
                    float newPosition = tooltipXPosition + (float)e.TotalX;

                    newPosition = Math.Clamp(newPosition, 0, (float)graphWidth);

                    int newIndex = (int)Math.Round(newPosition / pointSpacing);
                    newIndex = Math.Clamp(newIndex, 0, EnhancedGraphDrawable.DataPoints.Length - 1);

                    if (newIndex != currentDataPointIndex)
                    {
                        currentDataPointIndex = newIndex;
                        PositionTooltipAndLine(currentDataPointIndex);
                    }
                    break;
            }
        }

        private void OnGraphTapped(object? sender, TappedEventArgs e)
        {
            var graphView = this.FindByName<GraphicsView>("GraphView");
            if (graphView == null) return;

            var graphWidth = graphView.Width;
            if (graphWidth <= 0) return;

            var tapPosition = e.GetPosition(graphView);
            if (!tapPosition.HasValue) return;

            float pointSpacing = (float)(graphWidth / (EnhancedGraphDrawable.DataPoints.Length - 1));
            int newIndex = (int)Math.Round(tapPosition.Value.X / pointSpacing);
            newIndex = Math.Clamp(newIndex, 0, EnhancedGraphDrawable.DataPoints.Length - 1);

            currentDataPointIndex = newIndex;
            PositionTooltipAndLine(currentDataPointIndex);
        }

        private void OnGraphStartInteraction(object? sender, TouchEventArgs e)
        {
            if (e.Touches?.Length > 0)
            {
                isPointerTracking = true;
                UpdateTooltipPosition(e.Touches[0].X);
            }
        }

        private void OnGraphDragInteraction(object? sender, TouchEventArgs e)
        {
            if (e.Touches?.Length > 0)
            {
                UpdateTooltipPosition(e.Touches[0].X);
            }
        }

        private void OnGraphEndInteraction(object? sender, TouchEventArgs e)
        {
            isPointerTracking = false;
        }

        private void UpdateTooltipPosition(float xPosition)
        {
            var graphView = this.FindByName<GraphicsView>("GraphView");
            if (graphView == null) return;

            var graphWidth = graphView.Width;
            if (graphWidth <= 0) return;

            float pointSpacing = (float)(graphWidth / (EnhancedGraphDrawable.DataPoints.Length - 1));
            int newIndex = (int)Math.Round(xPosition / pointSpacing);
            newIndex = Math.Clamp(newIndex, 0, EnhancedGraphDrawable.DataPoints.Length - 1);

            if (newIndex != currentDataPointIndex)
            {
                currentDataPointIndex = newIndex;
                PositionTooltipAndLine(currentDataPointIndex);
            }
        }
    }

    // Classes pour le dessin (inchangées)
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

    public class EnhancedGraphDrawable : IDrawable
    {
        public static readonly float[] DataPoints = new float[]
        {
            0.35f, 0.45f, 0.65f, 0.75f, 0.85f, 0.60f, 0.45f, 0.40f, 0.50f, 0.60f, 0.45f, 0.55f, 0.40f, 0.60f
        };

        public int CurrentTooltipIndex { get; set; } = 5;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float height = dirtyRect.Height;
            float width = dirtyRect.Width;

            canvas.StrokeColor = Color.FromArgb("#DDDDDD");
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = new float[] { 4, 4 };

            for (int i = 0; i <= 8; i++)
            {
                float y = (i * height / 8);
                canvas.DrawLine(0, y, width, y);
            }

            if (DataPoints.Length > 1)
            {
                var path = new PathF();
                path.MoveTo(0, height);

                float x = 0;
                float y = height - (DataPoints[0] * height);
                path.LineTo(x, y);

                float pointSpacing = width / (DataPoints.Length - 1);
                for (int i = 1; i < DataPoints.Length; i++)
                {
                    x = i * pointSpacing;
                    y = height - (DataPoints[i] * height);
                    path.LineTo(x, y);
                }

                path.LineTo(width, height);
                path.LineTo(0, height);

                canvas.FillColor = Color.FromArgb("#FFD602");
                canvas.FillPath(path);

                canvas.StrokeColor = Color.FromArgb("#FFD602");
                canvas.StrokeSize = 2;
                canvas.StrokeDashPattern = null;

                var linePath = new PathF();
                linePath.MoveTo(0, height - (DataPoints[0] * height));
                for (int i = 1; i < DataPoints.Length; i++)
                {
                    x = i * pointSpacing;
                    y = height - (DataPoints[i] * height);
                    linePath.LineTo(x, y);
                }
                canvas.DrawPath(linePath);

                canvas.StrokeColor = Color.FromArgb("#DDDDDD");
                canvas.StrokeSize = 1;
                canvas.StrokeDashPattern = new float[] { 4, 4 };

                for (int i = 0; i <= DataPoints.Length; i++)
                {
                    float lineX = i * pointSpacing;
                    canvas.DrawLine(lineX, 0, lineX, height);
                }

                if (CurrentTooltipIndex < DataPoints.Length)
                {
                    float tooltipX = CurrentTooltipIndex * pointSpacing;
                    canvas.StrokeColor = Color.FromArgb("#AAAAAA");
                    canvas.StrokeSize = 1.5f;
                    canvas.DrawLine(tooltipX, 0, tooltipX, height);

                    float tooltipY = height - (DataPoints[CurrentTooltipIndex] * height);
                    canvas.FillColor = Colors.Black;
                    canvas.FillCircle(tooltipX, tooltipY, 4);
                }
            }
        }
    }
}
