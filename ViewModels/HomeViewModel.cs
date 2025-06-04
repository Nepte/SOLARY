using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SOLARY.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private string _userName = string.Empty;
        private string _todayDate = string.Empty;
        private double _currentKwh;
        private int _solarUsagePercent;
        private double _solarUsageProgress;
        private double _totalEnergy;
        private double _usedEnergy;
        private double _capacity;
        private double _co2Reduction;
        private double _panelGenerated;
        private bool _isDirectMode = true;
        private IDrawable? _graphDrawable;

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string TodayDate
        {
            get => _todayDate;
            set => SetProperty(ref _todayDate, value);
        }

        public double CurrentKwh
        {
            get => _currentKwh;
            set => SetProperty(ref _currentKwh, value);
        }

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

        public HomeViewModel()
        {
            // Initialiser avec des valeurs par défaut
            UserName = "Charlie";
            TodayDate = DateTime.Now.ToString("dd/MM/yyyy");
            CurrentKwh = 30.276;
            SolarUsagePercent = 40;
            SolarUsageProgress = 0.4;
            TotalEnergy = 36.2;
            UsedEnergy = 28.2;
            Capacity = 42.0;
            Co2Reduction = 28.2;
            PanelGenerated = 140.65;
            IsDirectMode = true;
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

    // Convertisseur pour changer la couleur en fonction du mode Direct/Indirect
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
}
