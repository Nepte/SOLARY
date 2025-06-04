using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using SOLARY.ViewModels;

namespace SOLARY.Views
{
    public partial class HomePage : ContentPage
    {
        private int currentDataPointIndex = 5; // Position initiale du tooltip (Feb 22)
        private float tooltipXPosition;
        private bool isPointerTracking = false;

        public HomePage()
        {
            InitializeComponent();

            // Si vous n'avez pas encore créé le GraphDrawable dans le ViewModel
            if (BindingContext is HomeViewModel viewModel)
            {
                viewModel.UserName = "Charlie";
                viewModel.TodayDate = DateTime.Now.ToString("dd/MM/yyyy");
                viewModel.CurrentKwh = 30.276;
                viewModel.SolarUsagePercent = 40;
                viewModel.SolarUsageProgress = 0.4;
                viewModel.TotalEnergy = 36.2;
                viewModel.UsedEnergy = 28.2;
                viewModel.Capacity = 42.0;
                viewModel.Co2Reduction = 28.2;
                viewModel.PanelGenerated = 140.65;
                viewModel.IsDirectMode = true;

                // Initialiser le graphique avec les données réelles
                InitializeGraph();
            }

            // Ajouter des gestionnaires d'événements pour le mouvement de la souris
            if (GraphView != null)
            {
                GraphView.StartInteraction += OnGraphStartInteraction;
                GraphView.DragInteraction += OnGraphDragInteraction;
                GraphView.EndInteraction += OnGraphEndInteraction;
            }

            // Ajouter un gestionnaire pour le mouvement du pointeur
#if WINDOWS
            Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(() => {
                var nativeView = GraphView?.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
                if (nativeView != null)
                {
                    nativeView.PointerMoved += (s, e) => {
                        if (isPointerTracking)
                        {
                            var point = e.GetCurrentPoint(nativeView);
                            UpdateTooltipPosition((float)point.Position.X);
                        }
                    };
                    
                    nativeView.PointerEntered += (s, e) => {
                        isPointerTracking = true;
                    };
                    
                    nativeView.PointerExited += (s, e) => {
                        isPointerTracking = false;
                    };
                }
            });
#endif

            // Ajouter les gestionnaires d'événements pour la navigation
            SetupNavigation();
        }

        private void SetupNavigation()
        {
            // Gestionnaire pour l'onglet Localiser
            var locateTab = this.FindByName<VerticalStackLayout>("LocaliserTab");
            if (locateTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PushAsync(new MapPage());
                };
                locateTab.GestureRecognizers.Add(tapGesture);
            }

            // Gestionnaire pour l'onglet Statistiques
            var statsTab = this.FindByName<VerticalStackLayout>("StatistiquesTab");
            if (statsTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await DisplayAlert("Statistiques", "La page Statistiques n'est pas encore implémentée.", "OK");
                };
                statsTab.GestureRecognizers.Add(tapGesture);
            }

            // Gestionnaire pour l'onglet Code (remplace Scan)
            var codeTab = this.FindByName<VerticalStackLayout>("CodeTab");
            if (codeTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PushAsync(new CodePage());
                };
                codeTab.GestureRecognizers.Add(tapGesture);
            }

            // Gestionnaire pour l'onglet Paramètres
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
            if (BindingContext is HomeViewModel viewModel)
            {
                viewModel.GraphDrawable = new EnhancedGraphDrawable();

                // Positionner le tooltip et la ligne verticale initialement
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () =>
                {
                    PositionTooltipAndLine(currentDataPointIndex);
                });
            }
        }

        private void PositionTooltipAndLine(int dataPointIndex)
        {
            var graphView = this.FindByName<GraphicsView>("GraphView");
            if (graphView == null) return;

            var graphWidth = graphView.Width;
            if (graphWidth <= 0) return;

            // Calculer la position X en fonction de l'index
            float pointSpacing = (float)(graphWidth / (EnhancedGraphDrawable.DataPoints.Length - 1));
            tooltipXPosition = dataPointIndex * pointSpacing;

            // Mettre à jour la position de la ligne verticale dans le GraphDrawable
            if (BindingContext is HomeViewModel viewModel && viewModel.GraphDrawable is EnhancedGraphDrawable graphDrawable)
            {
                graphDrawable.CurrentTooltipIndex = dataPointIndex;
                graphView.Invalidate(); // Redessiner le graphique
            }

            // Positionner le tooltip
            var tooltipFrame = this.FindByName<Border>("TooltipFrame");
            if (tooltipFrame != null)
            {
                tooltipFrame.TranslationX = tooltipXPosition - (tooltipFrame.Width / 2);
            }

            // Mettre à jour le contenu du tooltip
            UpdateTooltip(dataPointIndex);
        }

        private void UpdateTooltip(int dataPointIndex)
        {
            if (dataPointIndex < 0 || dataPointIndex >= EnhancedGraphDrawable.DataPoints.Length)
                return;

            // Mettre à jour le texte du tooltip
            string[] dates = { "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00", "17:30", "18:00", "18:30", "19:00", "19:30" };
            string date = dataPointIndex < dates.Length ? dates[dataPointIndex] : "Feb, 22";

            // Calculer une valeur plus réaliste basée sur la position dans le graphique
            int value = (int)(EnhancedGraphDrawable.DataPoints[dataPointIndex] * 200); // Valeur max 200KWh

            var tooltipDate = this.FindByName<Label>("TooltipDate");
            var tooltipValue = this.FindByName<Label>("TooltipValue");

            if (tooltipDate != null)
                tooltipDate.Text = "Feb, 22";

            if (tooltipValue != null)
                tooltipValue.Text = $"{value}kwh";
        }

        private void OnGraphPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    // Calculer le nouvel index en fonction du déplacement
                    var graphView = this.FindByName<GraphicsView>("GraphView");
                    if (graphView == null) return;

                    var graphWidth = graphView.Width;
                    if (graphWidth <= 0) return;

                    float pointSpacing = (float)(graphWidth / (EnhancedGraphDrawable.DataPoints.Length - 1));
                    float newPosition = tooltipXPosition + (float)e.TotalX;

                    // Limiter la position dans les bornes du graphique
                    newPosition = Math.Clamp(newPosition, 0, (float)graphWidth);

                    // Calculer l'index le plus proche
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
            // Calculer l'index en fonction de la position du tap
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

        // Méthodes pour gérer le mouvement de la souris sur le graphique
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

        private void OnSwitchToMainToggled(object? sender, ToggledEventArgs e)
        {
            // Logique pour switcher sur le "main electricity"
            if (e.Value)
            {
                // Activé
                DisplayAlert("Switch", "Vous avez activé l'électricité du réseau.", "OK");
            }
            else
            {
                // Désactivé
                DisplayAlert("Switch", "Vous avez désactivé l'électricité du réseau.", "OK");
            }
        }

        private void OnDirectClicked(object? sender, EventArgs e)
        {
            if (BindingContext is HomeViewModel vm)
                vm.IsDirectMode = true;
        }

        private void OnIndirectClicked(object? sender, EventArgs e)
        {
            if (BindingContext is HomeViewModel vm)
                vm.IsDirectMode = false;
        }
    }

    // Classe améliorée pour dessiner le graphique
    public class EnhancedGraphDrawable : IDrawable
    {
        // Données du graphique qui correspondent à l'image
        // Ces valeurs sont ajustées pour correspondre au graphique de l'image
        public static readonly float[] DataPoints = new float[]
        {
            0.35f, 0.45f, 0.65f, 0.75f, 0.85f, 0.60f, 0.45f, 0.40f, 0.50f, 0.60f, 0.45f, 0.55f, 0.40f, 0.60f
        };

        // Index actuel pour le tooltip
        public int CurrentTooltipIndex { get; set; } = 5;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float height = dirtyRect.Height;
            float width = dirtyRect.Width;

            // Dessiner les lignes horizontales pointillées
            canvas.StrokeColor = Color.FromArgb("#DDDDDD");
            canvas.StrokeSize = 1;
            canvas.StrokeDashPattern = new float[] { 4, 4 };

            // Dessiner plus de lignes horizontales pour une meilleure lecture
            for (int i = 0; i <= 8; i++)
            {
                float y = (i * height / 8);
                canvas.DrawLine(0, y, width, y);
            }

            // Dessiner le graphique avec un jaune uniforme
            if (DataPoints.Length > 1)
            {
                // Créer le chemin pour la zone sous la courbe
                var path = new PathF();

                // Commencer en bas à gauche
                path.MoveTo(0, height);

                // Ajouter le premier point
                float x = 0;
                float y = height - (DataPoints[0] * height);
                path.LineTo(x, y);

                // Ajouter le reste des points
                float pointSpacing = width / (DataPoints.Length - 1);
                for (int i = 1; i < DataPoints.Length; i++)
                {
                    x = i * pointSpacing;
                    y = height - (DataPoints[i] * height);
                    path.LineTo(x, y);
                }

                // Compléter le chemin vers le bas à droite puis vers le bas à gauche
                path.LineTo(width, height);
                path.LineTo(0, height);

                // Utiliser une couleur jaune uniforme comme dans l'image
                canvas.FillColor = Color.FromArgb("#FFD602");
                canvas.FillPath(path);

                // Dessiner la ligne du graphique en jaune légèrement plus foncé
                canvas.StrokeColor = Color.FromArgb("#FFD602");
                canvas.StrokeSize = 2;
                canvas.StrokeDashPattern = null; // Ligne continue pour le contour

                // Créer un chemin pour la ligne du graphique (sans la partie qui ferme le chemin)
                var linePath = new PathF();
                linePath.MoveTo(0, height - (DataPoints[0] * height));
                for (int i = 1; i < DataPoints.Length; i++)
                {
                    x = i * pointSpacing;
                    y = height - (DataPoints[i] * height);
                    linePath.LineTo(x, y);
                }
                canvas.DrawPath(linePath);

                // Dessiner les lignes verticales pointillées pour chaque point de données
                canvas.StrokeColor = Color.FromArgb("#DDDDDD");
                canvas.StrokeSize = 1;
                canvas.StrokeDashPattern = new float[] { 4, 4 };

                // Dessiner plus de lignes verticales pour une meilleure lecture
                for (int i = 0; i <= DataPoints.Length; i++)
                {
                    float lineX = i * pointSpacing;
                    canvas.DrawLine(lineX, 0, lineX, height);
                }

                // Dessiner la ligne verticale pointillée pour le tooltip (en plus foncé)
                if (CurrentTooltipIndex < DataPoints.Length)
                {
                    float tooltipX = CurrentTooltipIndex * pointSpacing;
                    canvas.StrokeColor = Color.FromArgb("#AAAAAA");
                    canvas.StrokeSize = 1.5f;
                    canvas.DrawLine(tooltipX, 0, tooltipX, height);

                    // Dessiner un point noir à la position du tooltip
                    float tooltipY = height - (DataPoints[CurrentTooltipIndex] * height);
                    canvas.FillColor = Colors.Black;
                    canvas.FillCircle(tooltipX, tooltipY, 4);
                }
            }
        }
    }
}
