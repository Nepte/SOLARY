using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using SOLARY.Model;
using SOLARY.Services;

namespace SOLARY.ViewModels
{
    // Classe BorneModel qui hérite de Borne - toutes les propriétés nécessaires sont déjà dans la classe de base
    public class BorneModel : Borne
    {
        // Méthode de débogage pour vérifier les coordonnées
        public override string ToString()
        {
            return $"Borne {BorneId}: {Name} - Lat: {Latitude}, Lng: {Longitude}, Adresse: {FullAddress}";
        }
    }

    // Utiliser INotifyPropertyChanged au lieu de BindableObject pour une meilleure compatibilité
    public class MapViewModel : INotifyPropertyChanged
    {
        private readonly BorneService _borneService;
        private ICommand? _zoomToStationCommand;

        public ObservableCollection<BorneModel> Bornes { get; set; } = new ObservableCollection<BorneModel>();

        public ICommand? ZoomToStationCommand
        {
            get => _zoomToStationCommand;
            set
            {
                _zoomToStationCommand = value;
                OnPropertyChanged();
            }
        }

        public MapViewModel()
        {
            _borneService = new BorneService();

            // Utiliser notre implémentation personnalisée de ICommand
            _zoomToStationCommand = new DelegateCommand((obj) =>
            {
                if (obj is int id)
                {
                    // Implémentation vide pour l'instant
                }
            });

            // Charger les bornes au démarrage
            Task.Run(async () => await LoadBornes());
        }

        // Méthode pour charger les bornes depuis l'API
        public async Task LoadBornes()
        {
            try
            {
                Debug.WriteLine("[INFO] Chargement des bornes...");
                var bornes = await _borneService.GetAllBornes();

                if (bornes != null)
                {
                    // Utiliser MainThread pour mettre à jour l'ObservableCollection
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Bornes.Clear();
                        foreach (var borne in bornes)
                        {
                            // Convertir Borne en BorneModel
                            var borneModel = new BorneModel
                            {
                                BorneId = borne.BorneId,
                                Name = borne.Name,
                                Address = borne.Address,
                                City = borne.City,
                                PostalCode = borne.PostalCode,
                                Latitude = borne.Latitude,
                                Longitude = borne.Longitude,
                                PowerOutput = borne.PowerOutput,
                                ChargePercentage = borne.ChargePercentage,
                                Status = borne.Status,
                                IsInMaintenance = borne.IsInMaintenance,
                                CreatedAt = borne.CreatedAt,
                                LastUsedAt = borne.LastUsedAt,
                                Distance = borne.Distance,
                                Casiers = borne.Casiers ?? new List<Casier>()
                            };

                            // Déboguer les coordonnées
                            Debug.WriteLine($"[DEBUG] {borneModel}");

                            Bornes.Add(borneModel);
                        }
                    });

                    Debug.WriteLine($"[INFO] {Bornes.Count} bornes chargées avec succès");
                }
                else
                {
                    Debug.WriteLine("[ERREUR] Aucune borne retournée par le service");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Chargement des bornes : {ex.Message}");
            }
        }

        // Méthode pour générer le JSON des bornes pour la carte
        public string GetBornesJson()
        {
            try
            {
                var bornesArray = new System.Text.StringBuilder();
                bornesArray.Append("[");

                for (int i = 0; i < Bornes.Count; i++)
                {
                    var borne = Bornes[i];

                    // Déterminer le statut pour la carte
                    string status = "Disponible";
                    if (borne.IsInMaintenance) status = "Maintenance";
                    else if (!borne.IsAvailable) status = "Occupée";

                    // Formater l'adresse complète de manière cohérente
                    string address = borne.FullAddress;

                    // Créer l'objet JSON pour cette borne
                    bornesArray.Append("{");
                    bornesArray.Append($"\"id\": {borne.BorneId},");
                    bornesArray.Append($"\"name\": \"{EscapeJsonString(borne.Name)}\",");
                    bornesArray.Append($"\"address\": \"{EscapeJsonString(address)}\",");
                    bornesArray.Append($"\"lat\": {borne.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                    bornesArray.Append($"\"lng\": {borne.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                    bornesArray.Append($"\"power\": \"{borne.PowerOutput.ToString(System.Globalization.CultureInfo.InvariantCulture)}\",");
                    bornesArray.Append($"\"status\": \"{status}\",");
                    bornesArray.Append($"\"percentage\": {borne.ChargePercentage},");

                    // Ajouter la distance si disponible
                    if (borne.Distance.HasValue)
                    {
                        bornesArray.Append($"\"distance\": {borne.Distance.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    }
                    else
                    {
                        bornesArray.Append("\"distance\": 0");
                    }

                    bornesArray.Append("}");

                    // Ajouter une virgule si ce n'est pas le dernier élément
                    if (i < Bornes.Count - 1)
                    {
                        bornesArray.Append(",");
                    }
                }

                bornesArray.Append("]");
                return bornesArray.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Génération du JSON des bornes : {ex.Message}");
                return "[]";
            }
        }

        // Méthode pour échapper les caractères spéciaux dans les chaînes JSON
        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }

        // Méthode pour rafraîchir les bornes
        public async Task RefreshBornes()
        {
            await LoadBornes();
        }

        // Méthode pour obtenir une borne par son ID
        public BorneModel? GetBorneById(int borneId)
        {
            return Bornes.FirstOrDefault(b => b.BorneId == borneId);
        }

        // Méthode pour filtrer les bornes par statut
        public List<BorneModel> GetBornesByStatus(string status)
        {
            return Bornes.Where(b => b.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Méthode pour obtenir les bornes disponibles
        public List<BorneModel> GetAvailableBornes()
        {
            return Bornes.Where(b => b.IsAvailable).ToList();
        }

        // Méthode pour obtenir les bornes en maintenance
        public List<BorneModel> GetMaintenanceBornes()
        {
            return Bornes.Where(b => b.IsInMaintenance).ToList();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    // Implémentation personnalisée de ICommand compatible avec toutes les plateformes
    public class DelegateCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
