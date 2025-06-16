using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SOLARY.Services;
using SOLARY.Model;

namespace SOLARY.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly IMesureService _mesureService;
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
        private IDrawable? _graphDrawable;
        private System.Timers.Timer? _dataUpdateTimer;

        // PROPRIÉTÉS DE COMPATIBILITÉ POUR L'ANCIEN CODE
        private int _solarUsagePercent;
        private double _solarUsageProgress;
        private double _totalEnergy;
        private double _usedEnergy;
        private double _capacity;

        public HomeViewModel() : this(null) { }

        public HomeViewModel(IMesureService? mesureService)
        {
            _mesureService = mesureService ?? new MesureService();
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

        // NOUVELLES PROPRIÉTÉS POUR LA BATTERIE
        public int BatteryLevel
        {
            get => _batteryLevel;
            set
            {
                if (SetProperty(ref _batteryLevel, value))
                {
                    BatteryProgress = value / 100.0;
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

        public IDrawable? GraphDrawable
        {
            get => _graphDrawable;
            set => SetProperty(ref _graphDrawable, value);
        }

        private async void InitializeData()
        {
            // Valeurs par défaut basées sur les VRAIES données de la borne_id = 1
            CurrentKwh = 30.276;
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

            // Charger les VRAIES données depuis l'API
            if (_mesureService != null)
            {
                await LoadLatestMeasureData();
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

                    // Calculer CurrentKwh basé sur la puissance réelle
                    CurrentKwh = (Power * 4.32) / 1000.0; // Conversion en kWh

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
            // Formule exacte selon ChatGPT (même calcul que MesureEnergie)
            double dureeHeures = 30.0 / 3600.0; // 30 secondes = 0.00833 h
            double energieWh = powerWatts * dureeHeures; // En Wh
            double energieKwh = energieWh / 1000.0; // Conversion Wh → kWh
            double co2EviteGrammes = energieKwh * 400.0; // 400g CO2/kWh

            return Math.Round(co2EviteGrammes, 6); // En grammes avec 6 décimales
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
