using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SOLARY.Services;
using SOLARY.Model;
using System.Linq;

namespace SOLARY.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly IMesureService _mesureService;
        private readonly BorneService _borneService;
        private string _todayDate = string.Empty;
        private double _currentKwh;
        private int _batteryLevel;
        private double _batteryProgress;
        private double _voltage;
        private double _current;
        private double _power;
        private double _co2Reduction;
        private double _panelGenerated;
        private bool _isDirectMode = true;
        private System.Timers.Timer? _dataUpdateTimer;

        // PROPRIÉTÉS DE COMPATIBILITÉ POUR L'ANCIEN CODE
        private int _solarUsagePercent;
        private double _solarUsageProgress;
        private double _totalEnergy;
        private double _usedEnergy;
        private double _capacity;

        private double _progressBarWidth = 0;

        // Nouvelle propriété pour le graphique à métrique unique
        private IDrawable? _singleMetricChartDrawable;
        private int _currentMetricIndex = 0; // 0=Voltage, 1=Current, 2=Power, 3=Battery

        // Historique des données pour le graphique
        private List<MesureEnergie> _mesuresHistory = new();

        public HomeViewModel() : this(null, null) { }

        public HomeViewModel(IMesureService? mesureService, BorneService? borneService)
        {
            _mesureService = mesureService ?? new MesureService();
            _borneService = borneService ?? new BorneService();
            InitializeData();
            if (_mesureService != null)
            {
                StartDataUpdateTimer();
            }
        }

        public string FormattedDate
        {
            get
            {
                DateTime now = DateTime.Now;
                string frenchMonth = GetFrenchMonth(now.Month);
                return $"{now.Day} {frenchMonth} {now.Year}";
            }
        }

        private string GetFrenchMonth(int month)
        {
            return month switch
            {
                1 => "Janvier",
                2 => "Février",
                3 => "Mars",
                4 => "Avril",
                5 => "Mai",
                6 => "Juin",
                7 => "Juillet",
                8 => "Août",
                9 => "Septembre",
                10 => "Octobre",
                11 => "Novembre",
                12 => "Décembre",
                _ => string.Empty
            };
        }

        public double CurrentKwh
        {
            get => _currentKwh;
            set => SetProperty(ref _currentKwh, value);
        }

        // Propriété formatée pour l'affichage
        public string FormattedCurrentKwh => $"{CurrentKwh:F1}";

        // NOUVELLES PROPRIÉTÉS POUR LA BATTERIE
        public int BatteryLevel
        {
            get => _batteryLevel;
            set
            {
                if (SetProperty(ref _batteryLevel, value))
                {
                    BatteryProgress = value / 100.0;
                    // Calculer la largeur de la barre pour une largeur de conteneur de 335px (375-40 padding)
                    ProgressBarWidth = Math.Max(0, (335.0 * value / 100.0));
                    // Synchroniser avec les anciennes propriétés pour compatibilité
                    SolarUsagePercent = value;
                    SolarUsageProgress = value / 100.0;
                    OnPropertyChanged(nameof(IsLowBattery));
                }
            }
        }

        public bool IsLowBattery => BatteryLevel < 30;

        public double BatteryProgress
        {
            get => _batteryProgress;
            set => SetProperty(ref _batteryProgress, value);
        }

        public double ProgressBarWidth
        {
            get => _progressBarWidth;
            set => SetProperty(ref _progressBarWidth, value);
        }

        // PROPRIÉTÉS DE COMPATIBILITÉ AVEC L'ANCIEN CODE
        public int SolarUsagePercent
        {
            get => _solarUsagePercent;
            set => SetProperty(ref _solarUsagePercent, value);
        }

        public double SolarUsageProgress
        {
            get => _solarUsageProgress;
            set => SetProperty(ref _solarUsageProgress, value);
        }

        public double TotalEnergy
        {
            get => _totalEnergy;
            set => SetProperty(ref _totalEnergy, value);
        }

        public double UsedEnergy
        {
            get => _usedEnergy;
            set => SetProperty(ref _usedEnergy, value);
        }

        public double Capacity
        {
            get => _capacity;
            set => SetProperty(ref _capacity, value);
        }

        // NOUVELLES PROPRIÉTÉS POUR LES MESURES
        public double Voltage
        {
            get => _voltage;
            set => SetProperty(ref _voltage, value);
        }

        public double Current
        {
            get => _current;
            set => SetProperty(ref _current, value);
        }

        public double Power
        {
            get => _power;
            set => SetProperty(ref _power, value);
        }

        public double Co2Reduction
        {
            get => _co2Reduction;
            set => SetProperty(ref _co2Reduction, value);
        }

        public double PanelGenerated
        {
            get => _panelGenerated;
            set => SetProperty(ref _panelGenerated, value);
        }

        public bool IsDirectMode
        {
            get => _isDirectMode;
            set => SetProperty(ref _isDirectMode, value);
        }

        // Propriétés pour la navigation des métriques
        public IDrawable? SingleMetricChartDrawable
        {
            get => _singleMetricChartDrawable;
            set => SetProperty(ref _singleMetricChartDrawable, value);
        }

        public int CurrentMetricIndex
        {
            get => _currentMetricIndex;
            set
            {
                if (SetProperty(ref _currentMetricIndex, value))
                {
                    UpdateCurrentMetricDisplay();
                    UpdateSingleMetricChart();
                }
            }
        }

        public string CurrentMetricDisplayName
        {
            get
            {
                return CurrentMetricIndex switch
                {
                    0 => "Tension (V)",
                    1 => "Courant (A)",
                    2 => "Puissance (W)",
                    3 => "Batterie (%)",
                    _ => "Tension (V)"
                };
            }
        }

        public Color CurrentMetricColor
        {
            get
            {
                return CurrentMetricIndex switch
                {
                    0 => Color.FromArgb("#007AFF"), // Bleu pour Tension
                    1 => Color.FromArgb("#34C759"), // Vert pour Courant
                    2 => Color.FromArgb("#FF9500"), // Orange pour Puissance
                    3 => Color.FromArgb("#AF52DE"), // Violet pour Batterie
                    _ => Color.FromArgb("#007AFF")
                };
            }
        }

        public void NextMetric()
        {
            CurrentMetricIndex = (CurrentMetricIndex + 1) % 4;
        }

        public void PreviousMetric()
        {
            CurrentMetricIndex = CurrentMetricIndex == 0 ? 3 : CurrentMetricIndex - 1;
        }

        private void UpdateCurrentMetricDisplay()
        {
            OnPropertyChanged(nameof(CurrentMetricDisplayName));
            OnPropertyChanged(nameof(CurrentMetricColor));
        }

        private void InitializeSingleMetricChart()
        {
            UpdateSingleMetricChart();
        }

        private void UpdateSingleMetricChart()
        {
            var metricInfo = CurrentMetricIndex switch
            {
                0 => (Color.FromArgb("#007AFF"), "V", (Func<MesureEnergie, double>)(m => m.Voltage)),
                1 => (Color.FromArgb("#34C759"), "A", (Func<MesureEnergie, double>)(m => m.Current)),
                2 => (Color.FromArgb("#FF9500"), "W", (Func<MesureEnergie, double>)(m => m.Power)),
                3 => (Color.FromArgb("#AF52DE"), "%", (Func<MesureEnergie, double>)(m => (double)m.BatteryLevel)),
                _ => (Color.FromArgb("#007AFF"), "V", (Func<MesureEnergie, double>)(m => m.Voltage))
            };

            SingleMetricChartDrawable = new SingleMetricChartDrawable(metricInfo.Item1, metricInfo.Item2, _mesuresHistory, metricInfo.Item3);
        }

        private async void InitializeData()
        {
            // Valeurs par défaut basées sur les VRAIES données de la borne_id = 1
            CurrentKwh = 30.3; // Valeur par défaut arrondie
            BatteryLevel = 82;    // Valeur RÉELLE de la BDD (pas 67%)
            Voltage = 5.0;        // Constante pour borne_id = 1
            Current = 1.4;        // Variable pour borne_id = 1
            Power = 7.0;          // Variable pour borne_id = 1
            Co2Reduction = CalculateCo2Reduction(Power);
            System.Diagnostics.Debug.WriteLine($"🧮 CO2 calculé pour {Power}W = {Co2Reduction}g");
            PanelGenerated = 140.65;
            IsDirectMode = true;

            // Valeurs de compatibilité
            TotalEnergy = 36.2;
            UsedEnergy = 28.2;
            Capacity = 42.0;

            // Charger l'historique des données depuis l'API
            if (_mesureService != null)
            {
                await LoadMesuresHistory();
                await LoadLatestMeasureData();
            }

            // Charger les informations de la borne pour récupérer le power_output
            await LoadBorneInfo();

            // Initialiser le graphique à métrique unique avec les vraies données
            InitializeSingleMetricChart();
        }

        private async Task LoadBorneInfo()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔌 Chargement des informations de la borne_id = 1...");

                var borne = await _borneService.GetBorne(1);

                if (borne != null)
                {
                    // Utiliser le power_output de la borne pour calculer CurrentKwh
                    CurrentKwh = Math.Round(borne.PowerOutput, 1); // Arrondir à 1 décimale

                    System.Diagnostics.Debug.WriteLine($"✅ Borne trouvée - PowerOutput: {borne.PowerOutput} kWh");
                    System.Diagnostics.Debug.WriteLine($"📍 Borne: {borne.Name} à {borne.FullAddress}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Borne_id = 1 non trouvée, utilisation valeur par défaut");
                    CurrentKwh = 30.276; // Valeur par défaut
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement borne: {ex.Message}");
                CurrentKwh = 30.276; // Valeur par défaut en cas d'erreur
            }
        }

        private async Task LoadMesuresHistory()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Chargement de l'historique des mesures...");

                // Récupérer les mesures des dernières 24h (pas de limite fixe)
                _mesuresHistory = await _mesureService.GetMesuresHistoryForBorneAsync(1, 100); // Plus de données pour couvrir 24h

                System.Diagnostics.Debug.WriteLine($"📊 Historique chargé: {_mesuresHistory.Count} mesures");

                if (_mesuresHistory.Count > 0)
                {
                    var firstMeasure = _mesuresHistory.First();
                    var lastMeasure = _mesuresHistory.Last();

                    System.Diagnostics.Debug.WriteLine($"📅 Période couverte:");
                    System.Diagnostics.Debug.WriteLine($"   Du: {firstMeasure.MeasureDate:yyyy-MM-dd HH:mm:ss}");
                    System.Diagnostics.Debug.WriteLine($"   Au: {lastMeasure.MeasureDate:yyyy-MM-dd HH:mm:ss}");

                    // Afficher quelques exemples
                    foreach (var mesure in _mesuresHistory.Take(5))
                    {
                        System.Diagnostics.Debug.WriteLine($"   📊 {mesure.MeasureDate:HH:mm} - V:{mesure.Voltage:F1} A:{mesure.Current:F1} W:{mesure.Power:F1} B:{mesure.BatteryLevel}%");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Aucune donnée d'historique récupérée");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement historique: {ex.Message}");
                _mesuresHistory = new List<MesureEnergie>();
            }
        }

        public async Task LoadLatestMeasureData()
        {
            try
            {
                if (_mesureService == null) return;

                var latestMeasure = await _mesureService.GetLatestMesureForBorneAsync(1);

                if (latestMeasure != null)
                {
                    // IMPORTANT : Mettre à jour TOUTES les valeurs depuis la BDD
                    BatteryLevel = latestMeasure.BatteryLevel;
                    Voltage = latestMeasure.Voltage;
                    Current = latestMeasure.Current;
                    Power = latestMeasure.Power;
                    Co2Reduction = latestMeasure.Co2Reduction;

                    // Vérifier que le calcul CO2 est correct
                    double calculatedCo2 = CalculateCo2Reduction(Power);
                    System.Diagnostics.Debug.WriteLine($"🧮 CO2 BDD: {Co2Reduction}g vs Calculé: {calculatedCo2}g");

                    // Recharger l'historique complet pour avoir les données les plus récentes
                    await LoadMesuresHistory();

                    // Recharger les informations de la borne pour s'assurer d'avoir le bon power_output
                    await LoadBorneInfo();

                    // Mettre à jour le graphique avec les nouvelles données
                    UpdateSingleMetricChart();

                    System.Diagnostics.Debug.WriteLine($"✅ Données BDD chargées - Battery: {BatteryLevel}%, Voltage: {Voltage}V, Current: {Current}A, Power: {Power}W, CO2: {Co2Reduction}g");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Aucune mesure trouvée pour la borne_id = 1");

                    // Garder les valeurs par défaut si pas de données BDD
                    System.Diagnostics.Debug.WriteLine($"🔄 Utilisation valeurs par défaut - Battery: {BatteryLevel}%, Voltage: {Voltage}V, Current: {Current}A, Power: {Power}W");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement données borne_id=1: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"🔄 Utilisation valeurs par défaut - Battery: {BatteryLevel}%, Voltage: {Voltage}V, Current: {Current}A, Power: {Power}W");
            }
        }

        private void StartDataUpdateTimer()
        {
            _dataUpdateTimer = new System.Timers.Timer(10000);
            _dataUpdateTimer.Elapsed += async (s, e) =>
            {
                await LoadLatestMeasureData();
            };
            _dataUpdateTimer.Start();
        }

        private double CalculateCo2Reduction(double powerWatts)
        {
            // Formule améliorée identique à MesureEnergie
            if (powerWatts <= 0) return 0;

            // Calcul de l'énergie produite par heure (en kWh)
            double energieKwhParHeure = powerWatts / 1000.0; // Conversion W → kWh/h

            // CO2 évité par heure en grammes (facteur français)
            double co2EviteGrammesParHeure = energieKwhParHeure * 57.0; // 57g CO2/kWh

            // Conversion en milligrammes
            double co2EviteMgParHeure = co2EviteGrammesParHeure * 1000.0;

            // Valeur par minute pour affichage
            double co2EviteMgParMinute = co2EviteMgParHeure / 60.0;

            return Math.Round(co2EviteMgParMinute, 1);
        }

        public void Dispose()
        {
            _dataUpdateTimer?.Stop();
            _dataUpdateTimer?.Dispose();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    // Classe pour le graphique à métrique unique
    public class SingleMetricChartDrawable : IDrawable
    {
        private readonly List<MesureEnergie> _mesures;
        private readonly Color _lineColor;
        private readonly string _unit;
        private readonly Func<MesureEnergie, double> _valueSelector;
        private int _selectedPointIndex = -1;

        public SingleMetricChartDrawable(Color lineColor, string unit, List<MesureEnergie> mesures, Func<MesureEnergie, double> valueSelector)
        {
            _lineColor = lineColor;
            _unit = unit;
            _mesures = mesures ?? new List<MesureEnergie>();
            _valueSelector = valueSelector;
        }

        public void SetSelectedPoint(int index)
        {
            _selectedPointIndex = index;
        }

        public (double value, DateTime time, int index) GetPointAt(float x, float y, RectF bounds)
        {
            if (_mesures.Count < 2) return (0, DateTime.Now, -1);

            float chartWidth = bounds.Width - 80;
            float chartHeight = bounds.Height - 60;
            float chartX = 60;

            if (x < chartX || x > chartX + chartWidth) return (0, DateTime.Now, -1);

            float relativeX = x - chartX;
            float pointSpacing = chartWidth / Math.Max(1, _mesures.Count - 1);
            int index = (int)Math.Round(relativeX / pointSpacing);

            if (index >= 0 && index < _mesures.Count)
            {
                var mesure = _mesures[index];
                return (_valueSelector(mesure), mesure.MeasureDate, index);
            }

            return (0, DateTime.Now, -1);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_mesures.Count < 2)
            {
                // Afficher un message si pas assez de données
                canvas.FontColor = Color.FromArgb("#666666");
                canvas.FontSize = 14;
                canvas.DrawString("Pas assez de données disponibles",
                    new RectF(0, 0, dirtyRect.Width, dirtyRect.Height),
                    HorizontalAlignment.Center, VerticalAlignment.Center);
                return;
            }

            float width = dirtyRect.Width;
            float height = dirtyRect.Height;

            // Marges pour les axes
            float leftMargin = 60;
            float rightMargin = 20;
            float topMargin = 20;
            float bottomMargin = 40;

            float chartWidth = width - leftMargin - rightMargin;
            float chartHeight = height - topMargin - bottomMargin;
            float chartX = leftMargin;
            float chartY = topMargin;

            // Calculer les valeurs min/max pour cette métrique
            var values = _mesures.Select(_valueSelector).ToList();
            double minValue = values.Min();
            double maxValue = values.Max();
            double range = maxValue - minValue;

            // Appliquer des marges d'échelle selon le type de métrique
            double paddingPercent = _unit switch
            {
                "V" => range < 1.0 ? 0.5 : 0.2,    // Tension: marge de 50% si variation < 1V, sinon 20%
                "A" => range < 2.0 ? 0.4 : 0.2,    // Courant: marge de 40% si variation < 2A, sinon 20%
                "W" => range < 5.0 ? 0.3 : 0.15,   // Puissance: marge de 30% si variation < 5W, sinon 15%
                "%" => range < 10.0 ? 0.3 : 0.1,   // Batterie: marge de 30% si variation < 10%, sinon 10%
                _ => 0.2
            };

            // Calculer les nouvelles limites avec padding
            double padding = Math.Max(range * paddingPercent, range == 0 ? 1 : range * 0.1);
            minValue = minValue - padding;
            maxValue = maxValue + padding;
            range = maxValue - minValue;

            if (range == 0) range = 1;

            // Dessiner le fond du graphique
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(chartX, chartY, chartWidth, chartHeight);

            // Dessiner la grille
            canvas.StrokeColor = Color.FromArgb("#E8E8E8");
            canvas.StrokeSize = 0.5f;

            // Lignes horizontales (valeurs)
            for (int i = 0; i <= 4; i++)
            {
                float y = chartY + (i * chartHeight / 4);
                canvas.DrawLine(chartX, y, chartX + chartWidth, y);

                // Étiquettes de l'axe Y
                double value = maxValue - (i * range / 4);
                string format = _unit switch
                {
                    "V" => "F2",  // 2 décimales pour la tension (ex: 4.95V)
                    "A" => "F2",  // 2 décimales pour le courant (ex: 1.45A)
                    "W" => "F1",  // 1 décimale pour la puissance (ex: 7.2W)
                    "%" => "F0",  // Pas de décimale pour la batterie (ex: 82%)
                    _ => "F1"
                };
                canvas.FontColor = Color.FromArgb("#666666");
                canvas.FontSize = 10;
                canvas.DrawString($"{value.ToString(format)}", new RectF(5, y - 8, leftMargin - 10, 16), HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            // Lignes verticales et étiquettes de temps DYNAMIQUES basées sur les vraies données
            int timeSteps = Math.Min(6, _mesures.Count); // Maximum 6 étiquettes de temps

            // Calculer la durée totale couverte par les données
            var firstTime = _mesures.First().MeasureDate;
            var lastTime = _mesures.Last().MeasureDate;
            var totalDuration = lastTime - firstTime;

            System.Diagnostics.Debug.WriteLine($"📊 Graphique - Période: {firstTime:HH:mm} à {lastTime:HH:mm} ({totalDuration.TotalHours:F1}h)");

            for (int i = 0; i <= timeSteps; i++)
            {
                float x = chartX + (i * chartWidth / timeSteps);
                canvas.DrawLine(x, chartY, x, chartY + chartHeight);

                // Étiquettes de l'axe X (temps) - Basées sur les vraies données
                if (i < _mesures.Count)
                {
                    DateTime timeToShow;

                    if (i == 0)
                    {
                        timeToShow = firstTime;
                    }
                    else if (i == timeSteps)
                    {
                        timeToShow = lastTime;
                    }
                    else
                    {
                        // Interpoler entre le premier et le dernier temps
                        double ratio = (double)i / timeSteps;
                        var ticksToAdd = (long)(totalDuration.Ticks * ratio);
                        timeToShow = firstTime.AddTicks(ticksToAdd);
                    }

                    canvas.FontColor = Color.FromArgb("#666666");
                    canvas.FontSize = 10;

                    // Format d'affichage selon la durée
                    string timeFormat = totalDuration.TotalHours > 2 ? "HH:mm" : "HH:mm:ss";
                    canvas.DrawString(timeToShow.ToString(timeFormat),
                        new RectF(x - 25, chartY + chartHeight + 5, 50, 20),
                        HorizontalAlignment.Center, VerticalAlignment.Top);
                }
            }

            // Dessiner la ligne de données
            canvas.StrokeColor = _lineColor;
            canvas.StrokeSize = 3f;

            var path = new PathF();
            float pointSpacing = chartWidth / Math.Max(1, _mesures.Count - 1);

            for (int i = 0; i < _mesures.Count; i++)
            {
                float x = chartX + (i * pointSpacing);
                double value = _valueSelector(_mesures[i]);
                float normalizedValue = (float)((value - minValue) / range);
                float y = chartY + chartHeight - (normalizedValue * chartHeight);

                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }

            canvas.DrawPath(path);

            // Dessiner les points
            canvas.FillColor = _lineColor;
            for (int i = 0; i < _mesures.Count; i++)
            {
                float x = chartX + (i * pointSpacing);
                double value = _valueSelector(_mesures[i]);
                float normalizedValue = (float)((value - minValue) / range);
                float y = chartY + chartHeight - (normalizedValue * chartHeight);

                // Point normal ou sélectionné
                float radius = (i == _selectedPointIndex) ? 5 : 3f;
                canvas.FillCircle(x, y, radius);

                // Afficher la tooltip pour le point sélectionné
                if (i == _selectedPointIndex)
                {
                    canvas.FillColor = Color.FromArgb("#333333");
                    var time = _mesures[i].MeasureDate;
                    var tooltipText = $"{time:dd/MM HH:mm}\n{value:F1}{_unit}";

                    // Fond de la tooltip
                    canvas.FillRoundedRectangle(x - 35, y - 45, 70, 35, 6);

                    // Texte de la tooltip
                    canvas.FontColor = Colors.White;
                    canvas.FontSize = 11;
                    canvas.DrawString(tooltipText, new RectF(x - 35, y - 45, 70, 35), HorizontalAlignment.Center, VerticalAlignment.Center);

                    canvas.FillColor = _lineColor; // Restaurer la couleur
                }
            }

            // Dessiner les bordures du graphique
            canvas.StrokeColor = Color.FromArgb("#CCCCCC");
            canvas.StrokeSize = 1f;
            canvas.DrawRectangle(chartX, chartY, chartWidth, chartHeight);
        }
    }

    // Convertisseurs (inchangés)
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isDirectMode && parameter is string colors)
            {
                string[] colorOptions = colors.Split(':');
                if (colorOptions.Length == 2)
                {
                    return isDirectMode ? colorOptions[0] : colorOptions[1];
                }
            }
            return Colors.Black;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProgressWidthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double progress && parameter is double containerWidth)
            {
                return containerWidth * progress;
            }
            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BatteryToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int batteryLevel && parameter is string elementType)
            {
                bool isLowBattery = batteryLevel < 30;

                if (elementType == "bar")
                {
                    return isLowBattery ? Colors.Red : Color.FromArgb("#FFD602");
                }
                else if (elementType == "text")
                {
                    return isLowBattery ? Colors.Red : Color.FromArgb("#FFD602");
                }
                else if (elementType == "background")
                {
                    return isLowBattery ? Color.FromArgb("#FEECEB") : Color.FromArgb("#FFF8E0");
                }
            }
            return Color.FromArgb("#FFD602");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UsageToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int usagePercent && parameter is string elementType)
            {
                bool isHighUsage = usagePercent > 70;

                if (elementType == "bar")
                {
                    return isHighUsage ? Colors.Red : Color.FromArgb("#FFD602");
                }
                else if (elementType == "text")
                {
                    return isHighUsage ? Colors.Red : Color.FromArgb("#FFD602");
                }
                else if (elementType == "background")
                {
                    return isHighUsage ? Color.FromArgb("#FEECEB") : Color.FromArgb("#FFF8E0");
                }
            }
            return Color.FromArgb("#FFD602");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
