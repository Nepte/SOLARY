using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using SOLARY.ViewModels;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.ApplicationModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;

namespace SOLARY.Views
{
    public partial class MapPage : ContentPage
    {
        private MapViewModel? _viewModel;
        private bool _mapInitialized = false;
        private VerticalStackLayout? _stationListLayout;
        private int _selectedStationId = -1;
        private Dictionary<int, Border> _stationCards = new Dictionary<int, Border>();
        private bool _isFullListMode = false;

        // Ajouter ces variables de classe
        private ScrollView? _mainScrollView;
        private Grid? _mapContainer;
        private double _lastScrollY = 0;

        public MapPage()
        {
            InitializeComponent();

            // Récupérer la référence au layout des stations
            _stationListLayout = this.FindByName<VerticalStackLayout>("StationListLayout");

            _viewModel = new MapViewModel();
            BindingContext = _viewModel;

            if (_viewModel != null)
            {
                _viewModel.ZoomToStationCommand = new Command<int>(ZoomToStation);
            }

            // Configurer la navigation
            SetupNavigation();

            // Initialiser la carte après le chargement de la page
            this.Loaded += OnPageLoaded;

            // Ajouter un gestionnaire pour le défilement de la liste
            var stationListScroll = this.FindByName<ScrollView>("StationListScroll");
            if (stationListScroll != null)
            {
                //stationListScroll.Scrolled += OnStationListScrolled;
            }

            // Ajouter un gestionnaire pour le scroll principal
            var mainScrollView = this.FindByName<ScrollView>("MainScrollView");
            if (mainScrollView != null)
            {
                mainScrollView.Scrolled += OnMainScrolled;
            }
        }

        // Modifier la méthode OnMainScrolled pour gérer la visibilité de la barre de recherche
        private void OnMainScrolled(object? sender, ScrolledEventArgs e)
        {
            if (_mapContainer == null) return;

            // Ajouter un garde-fou pour éviter les boucles infinies
            if (Math.Abs(e.ScrollY - _lastScrollY) < 1)
            {
                return;
            }

            // Calculer la direction du scroll
            bool isScrollingDown = e.ScrollY > _lastScrollY;
            _lastScrollY = e.ScrollY;

            // Définir des limites claires pour la hauteur de la carte
            double minHeight = 100;
            double maxHeight = 450;
            double currentHeight = _mapContainer.HeightRequest;

            // Gérer la visibilité de la barre de recherche
            var searchBar = this.FindByName<SearchBar>("SearchBar");
            if (searchBar != null)
            {
                // Si on est en haut de la page, afficher la barre de recherche
                if (e.ScrollY < 50)
                {
                    searchBar.IsVisible = true;
                    searchBar.Opacity = 1;
                }
                // Si on descend, masquer progressivement la barre de recherche
                else if (isScrollingDown && e.ScrollY > 50)
                {
                    double opacity = Math.Max(0, 1 - ((e.ScrollY - 50) / 100));
                    searchBar.Opacity = opacity;
                    searchBar.IsVisible = opacity > 0.1;
                }
            }

            // Si on scrolle vers le bas et que la carte est visible
            if (isScrollingDown && e.ScrollY > 50)
            {
                // Réduire progressivement la hauteur de la carte, mais pas en dessous du minimum
                double newHeight = Math.Max(minHeight, maxHeight - (e.ScrollY - 50) * 0.5);

                // Ne mettre à jour que si le changement est significatif
                if (Math.Abs(newHeight - currentHeight) > 5)
                {
                    _mapContainer.HeightRequest = newHeight;
                }
            }
            // Si on scrolle vers le haut et qu'on est près du haut
            else if (!isScrollingDown && e.ScrollY < 200)
            {
                // Restaurer progressivement la hauteur de la carte, mais pas au-dessus du maximum
                double newHeight = Math.Min(maxHeight, minHeight + (200 - e.ScrollY) * 1.75);

                // Ne mettre à jour que si le changement est significatif
                if (Math.Abs(newHeight - currentHeight) > 5)
                {
                    _mapContainer.HeightRequest = newHeight;
                }
            }

            // Si on est tout en haut, restaurer complètement la carte
            if (e.ScrollY < 10)
            {
                _mapContainer.HeightRequest = maxHeight;
            }

            // Si on est tout en bas, fixer la hauteur minimale pour éviter les rebonds
            if (e.ScrollY > 1000)
            {
                _mapContainer.HeightRequest = minHeight;
            }
        }

        // Modifier la méthode OnPageLoaded pour initialiser les références
        private void OnPageLoaded(object? sender, EventArgs e)
        {
            InitializeMap();

            // Récupérer les références aux éléments UI
            _mainScrollView = this.FindByName<ScrollView>("MainScrollView");
            _mapContainer = this.FindByName<Grid>("MapContainer");

            // Modifier la couleur de fond de la WebView
            if (MapWebView != null)
            {
                MapWebView.BackgroundColor = Colors.Black;
            }

            // Ajouter les bornes à la liste après l'initialisation des composants
            if (_stationListLayout != null && _viewModel != null)
            {
                // S'assurer que la liste est vide avant d'ajouter les bornes
                _stationListLayout.Clear();
                _stationCards.Clear();

                // Trier les bornes par disponibilité (disponibles d'abord)
                var sortedBornes = _viewModel.Bornes.OrderByDescending(b => b.IsAvailable).ToList();

                // Ajouter les bornes à la liste
                foreach (var borne in sortedBornes)
                {
                    var stationCard = CreateStationCard(borne);
                    _stationListLayout.Add(stationCard);
                    _stationCards[borne.Id] = stationCard;
                }
            }

            // Configurer la WebView pour désactiver le scroll et les interactions qui pourraient interférer
            MapWebView?.EvaluateJavaScriptAsync(@"
                document.body.style.overflow = 'hidden';
        
                // Désactiver le scroll sur la carte
                if (map) {
                    map.scrollWheelZoom.disable();
                    map.dragging.disable();
                    map.touchZoom.disable();
                    map.doubleClickZoom.disable();
                    map.boxZoom.disable();
                    map.keyboard.disable();
                }
        
                // Réactiver uniquement via les boutons
                window.zoomIn = function() {
                    if (map) {
                        map.dragging.enable();
                        map.zoomIn();
                        setTimeout(function() { map.dragging.disable(); }, 100);
                    }
                };
        
                window.zoomOut = function() {
                    if (map) {
                        map.dragging.enable();
                        map.zoomOut();
                        setTimeout(function() { map.dragging.disable(); }, 100);
                    }
                };
            ");
        }

        // Nouvelle implémentation de CreateStationCard pour correspondre au design souhaité
        private Border CreateStationCard(BorneModel borne)
        {
            // Créer la bordure principale (carte)
            var stationBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#121824"),
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
                Margin = new Thickness(10, 5, 10, 5),
                StrokeThickness = 0,
                Shadow = new Shadow
                {
                    Brush = Color.FromArgb("#30000000"),
                    Offset = new Point(0, 2),
                    Radius = 4
                }
            };

            // Créer le contenu de la carte
            var contentGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // Pour l'icône de batterie
                    new ColumnDefinition { Width = GridLength.Star }, // Pour les informations
                    new ColumnDefinition { Width = GridLength.Auto }  // Pour le statut et la barre de progression
                },
                Padding = new Thickness(15, 15)
            };

            // Icône de batterie à gauche avec fond circulaire noir
            string batteryImageSource;
            Color batteryColor;
            if (borne.IsInMaintenance)
            {
                batteryImageSource = "battery_red.png";
                batteryColor = Color.FromArgb("#F44336"); // Rouge
            }
            else if (borne.IsAvailable)
            {
                batteryImageSource = "battery_yellow.png";
                batteryColor = Color.FromArgb("#FFD602"); // Jaune
            }
            else
            {
                batteryImageSource = "battery_grey.png";
                batteryColor = Color.FromArgb("#BBBBBB"); // Gris
            }

            // Créer un conteneur circulaire noir pour l'icône de batterie
            var batteryContainer = new Frame
            {
                BackgroundColor = Colors.Black,
                CornerRadius = 20, // Cercle
                HeightRequest = 40,
                WidthRequest = 40,
                Padding = 0,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };

            var batteryImage = new Image
            {
                Source = batteryImageSource,
                HeightRequest = 24,
                WidthRequest = 36,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

            batteryContainer.Content = batteryImage;
            contentGrid.Add(batteryContainer, 0, 0);

            // Informations de la station (colonne du milieu)
            var infoStack = new VerticalStackLayout
            {
                Spacing = 4
            };

            // Nom de la station
            var nameLabel = new Label
            {
                Text = borne.Name,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            infoStack.Add(nameLabel);

            // Adresse
            var addressLabel = new Label
            {
                Text = borne.Address,
                FontSize = 14,
                TextColor = Color.FromArgb("#BBBBBB"),
                LineBreakMode = LineBreakMode.TailTruncation
            };
            infoStack.Add(addressLabel);

            // Distance avec icône de localisation
            var distanceStack = new HorizontalStackLayout
            {
                Spacing = 6,
                Margin = new Thickness(0, 6, 0, 0)
            };

            var locationCircle = new Ellipse
            {
                Fill = Color.FromArgb("#BBBBBB"),
                HeightRequest = 10,
                WidthRequest = 10,
                VerticalOptions = LayoutOptions.Center
            };
            distanceStack.Add(locationCircle);

            var distanceLabel = new Label
            {
                Text = $"{borne.Distance:F1} km",
                FontSize = 14,
                TextColor = Color.FromArgb("#BBBBBB")
            };
            distanceStack.Add(distanceLabel);

            infoStack.Add(distanceStack);
            contentGrid.Add(infoStack, 1, 0);

            // Statut et barre de progression (colonne de droite)
            var statusStack = new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.End
            };

            // Statut
            string statusColor;
            string statusText;

            if (borne.IsInMaintenance)
            {
                statusColor = "#F44336"; // Rouge
                statusText = "Maintenance";
            }
            else if (borne.IsAvailable)
            {
                statusColor = "#FFD602"; // Jaune
                statusText = "Disponible";
            }
            else
            {
                statusColor = "#BBBBBB"; // Gris
                statusText = "Occupée";
            }

            var statusLabel = new Label
            {
                Text = statusText,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb(statusColor),
                HorizontalOptions = LayoutOptions.End
            };
            statusStack.Add(statusLabel);

            // Barre de progression avec pourcentage
            var progressStack = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto }
                },
                Margin = new Thickness(0, 4, 0, 0)
            };

            // Barre de progression plus épaisse
            var progressBarContainer = new Grid
            {
                HeightRequest = 6, // Augmenté de 4 à 6 pour être plus épais
                WidthRequest = 100,
                VerticalOptions = LayoutOptions.Center
            };

            var progressBackground = new Rectangle
            {
                Fill = Color.FromArgb("#333333"),
                WidthRequest = 100,
                HeightRequest = 6 // Augmenté de 4 à 6 pour être plus épais
            };

            var progressFill = new Rectangle
            {
                Fill = Color.FromArgb(statusColor),
                WidthRequest = 100 * borne.ChargePercentage / 100,
                HeightRequest = 6, // Augmenté de 4 à 6 pour être plus épais
                HorizontalOptions = LayoutOptions.Start
            };

            progressBarContainer.Add(progressBackground);
            progressBarContainer.Add(progressFill);
            progressStack.Add(progressBarContainer, 0, 0);

            // Pourcentage
            var percentLabel = new Label
            {
                Text = $"{borne.ChargePercentage}%",
                FontSize = 14,
                TextColor = Color.FromArgb("#BBBBBB"),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalOptions = LayoutOptions.Center
            };
            progressStack.Add(percentLabel, 1, 0);

            statusStack.Add(progressStack);
            contentGrid.Add(statusStack, 2, 0);

            stationBorder.Content = contentGrid;

            // Ajouter un effet visuel pour le survol
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => {
                SelectStation(borne.Id);
                ZoomToStation(borne.Id);
            };
            stationBorder.GestureRecognizers.Add(tapGesture);

            return stationBorder;
        }

        // Modifier la méthode SelectStation pour utiliser une bordure jaune
        private void SelectStation(int stationId)
        {
            // Désélectionner la station précédemment sélectionnée
            if (_selectedStationId != -1 && _stationCards.ContainsKey(_selectedStationId))
            {
                _stationCards[_selectedStationId].StrokeThickness = 0;
                _stationCards[_selectedStationId].Stroke = null;
            }

            // Sélectionner la nouvelle station
            _selectedStationId = stationId;
            if (_stationCards.ContainsKey(stationId))
            {
                _stationCards[stationId].StrokeThickness = 2;
                _stationCards[stationId].Stroke = Color.FromArgb("#FFD602");

                // Faire défiler vers le haut pour voir la carte
                _mainScrollView?.ScrollToAsync(0, 0, true);

                // Restaurer la hauteur de la carte
                if (_mapContainer != null)
                {
                    _mapContainer.HeightRequest = 450;
                }
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null || string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                // Si la recherche est vide, afficher toutes les bornes
                if (_viewModel != null)
                {
                    var sortedBornes = _viewModel.Bornes.OrderByDescending(b => b.IsAvailable).ToList();
                    UpdateStationList(sortedBornes);
                }
                return;
            }

            // Filtrer les bornes en fonction du texte de recherche
            var searchText = e.NewTextValue.ToLowerInvariant();
            var filteredBornes = _viewModel.Bornes.Where(b =>
                b.Name.ToLowerInvariant().Contains(searchText) ||
                b.Address.ToLowerInvariant().Contains(searchText)).ToList();

            // Mettre à jour la liste des bornes
            UpdateStationList(filteredBornes);
        }

        private void UpdateStationList(List<BorneModel> bornes)
        {
            // Vérifier que le layout existe
            if (_stationListLayout == null) return;

            // Vider la liste actuelle
            _stationListLayout.Clear();
            _stationCards.Clear();

            // Trier les bornes par disponibilité (disponibles d'abord)
            var sortedBornes = bornes.OrderByDescending(b => b.IsAvailable).ToList();

            // Ajouter les bornes à la liste
            foreach (var borne in sortedBornes)
            {
                var stationCard = CreateStationCard(borne);
                _stationListLayout.Add(stationCard);
                _stationCards[borne.Id] = stationCard;
            }

            // Réinitialiser la sélection
            _selectedStationId = -1;
        }

        private void InitializeMap()
        {
            try
            {
                // Charger le fichier HTML depuis les ressources
                string htmlContent = LoadHtmlFromResource("map.html");

                // Si le contenu est vide, afficher une alerte
                if (string.IsNullOrWhiteSpace(htmlContent))
                {
                    Debug.WriteLine("ERREUR: Contenu HTML non trouvé dans les ressources");
                    DisplayAlert("Erreur", "Impossible de charger la carte. Le fichier map.html n'a pas été trouvé.", "OK");
                    return;
                }

                // Configurer la WebView pour permettre JavaScript
#if ANDROID
                var handler = MapWebView?.Handler as Microsoft.Maui.Handlers.WebViewHandler;
                if (handler != null)
                {
                    var webView = handler.PlatformView as Android.Webkit.WebView;
                    if (webView != null)
                    {
                        webView.Settings.JavaScriptEnabled = true;
                        webView.Settings.DomStorageEnabled = true;
                        webView.Settings.AllowFileAccess = true;
                        webView.Settings.AllowContentAccess = true;
                        webView.Settings.AllowFileAccessFromFileURLs = true;
                        webView.Settings.AllowUniversalAccessFromFileURLs = true;
                        webView.Settings.SetSupportZoom(true);
                        webView.Settings.BuiltInZoomControls = true;
                        webView.Settings.DisplayZoomControls = false;
                        webView.Settings.LoadWithOverviewMode = true;
                        webView.Settings.UseWideViewPort = true;

                        // Effacer le cache
                        webView.ClearCache(true);

                        // Ajouter une interface JavaScript pour la communication
                        webView.AddJavascriptInterface(new WebViewJavaScriptInterface(this), "jsBridge");
                    }
                }
#endif

#if IOS || MACCATALYST
        // Configuration spécifique pour iOS
        var handler = MapWebView?.Handler as Microsoft.Maui.Handlers.WebViewHandler;
        if (handler != null)
        {
            var webView = handler.PlatformView as WebKit.WKWebView;
            if (webView != null)
            {
                // Effacer le cache - méthode compatible avec toutes les versions
                var dataTypes = WebKit.WKWebsiteDataStore.AllWebsiteDataTypes;

                // Utiliser une méthode qui ne dépend pas de NSDate
                WebKit.WKWebsiteDataStore.DefaultDataStore.RemoveDataOfTypes(
                    dataTypes,
                    Foundation.NSDate.DistantPast,
                    () => Debug.WriteLine("Cache cleared")
                );

                // Ajouter un script d'injection pour la communication
                var scriptContent = @"window.webkit.messageHandlers.jsBridge = { 
            postMessage: function(message) { 
                window.location.href = 'bridge://' + encodeURIComponent(JSON.stringify(message)); 
            } 
        };";

                var script = new WebKit.WKUserScript(
                    new Foundation.NSString(scriptContent),
                    WebKit.WKUserScriptInjectionTime.AtDocumentStart,
                    false
                );

                webView.Configuration.UserContentController.AddUserScript(script);
            }
        }
#endif

                // Générer les données des bornes en JSON
                string bornesJson = "[]";
                if (_viewModel != null)
                {
                    bornesJson = GenerateBornesJson();
                    Debug.WriteLine($"[DEBUG] Données des bornes générées: {bornesJson}");
                }

                // Remplacer les données statiques par les données dynamiques
                htmlContent = htmlContent.Replace("var stationData = [];", $"var stationData = {bornesJson};");

                // Ajouter un timestamp pour éviter la mise en cache
                string timestamp = DateTime.Now.Ticks.ToString();
                htmlContent = htmlContent.Replace("</head>",
                    $"<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\" />" +
                    $"<meta http-equiv=\"Pragma\" content=\"no-cache\" />" +
                    $"<meta http-equiv=\"Expires\" content=\"0\" />" +
                    $"<script>console.log('Version: {timestamp}');</script></head>");

                Debug.WriteLine($"Chargement de la carte avec timestamp: {timestamp}");

                // Charger le HTML dans la WebView
                if (MapWebView != null)
                {
                    MapWebView.Source = new HtmlWebViewSource { Html = htmlContent };

                    // Ajouter un gestionnaire d'événements pour la navigation
                    MapWebView.Navigated += OnWebViewNavigated;

                    // Ajouter un gestionnaire pour les erreurs JavaScript
                    MapWebView.Navigating += OnWebViewNavigating;

                    // S'assurer que la WebView est visible
                    MapWebView.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Erreur", $"Erreur lors de l'initialisation de la carte: {ex.Message}", "OK");
                Debug.WriteLine($"Erreur d'initialisation de la carte: {ex}");
            }
        }

        // Nouvelle méthode pour générer le JSON des bornes
        private string GenerateBornesJson()
        {
            if (_viewModel == null || _viewModel.Bornes == null || _viewModel.Bornes.Count == 0)
                return "[]";

            var bornesArray = new System.Text.StringBuilder();
            bornesArray.Append("[");

            for (int i = 0; i < _viewModel.Bornes.Count; i++)
            {
                var borne = _viewModel.Bornes[i];

                // Déterminer le statut pour la carte
                string status = "Disponible";
                if (borne.IsInMaintenance) status = "Maintenance";
                else if (!borne.IsAvailable) status = "Occupée";

                // Formater l'adresse complète
                string address = borne.FullAddress;

                // Créer l'objet JSON pour cette borne avec les coordonnées exactes
                bornesArray.Append("{");
                bornesArray.Append($"\"id\": {borne.Id},");
                bornesArray.Append($"\"name\": \"{EscapeJsonString(borne.Name)}\",");
                bornesArray.Append($"\"address\": \"{EscapeJsonString(address)}\",");
                // Utiliser les coordonnées exactes de la base de données
                bornesArray.Append($"\"lat\": {borne.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                bornesArray.Append($"\"lng\": {borne.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                bornesArray.Append($"\"power\": \"{borne.PowerOutput.ToString(System.Globalization.CultureInfo.InvariantCulture)}\",");
                bornesArray.Append($"\"status\": \"{status}\",");
                bornesArray.Append($"\"percentage\": {borne.ChargePercentage}");

                bornesArray.Append("}");

                // Ajouter une virgule si ce n'est pas le dernier élément
                if (i < _viewModel.Bornes.Count - 1)
                {
                    bornesArray.Append(",");
                }
            }

            bornesArray.Append("]");

            // Déboguer les coordonnées pour vérification
            Debug.WriteLine($"[DEBUG] JSON des bornes: {bornesArray.ToString()}");
            return bornesArray.ToString();
        }

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

        // Remplacer la méthode OnWebViewNavigated existante par celle-ci
        private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
        {
            Debug.WriteLine($"WebView navigated: {e.Result}");
            _mapInitialized = true;

            // Forcer un rafraîchissement de la carte après le chargement
            MapWebView?.EvaluateJavaScriptAsync(@"
        setTimeout(function() {
            console.log('Initialisation de la carte...');
            if (typeof initMap === 'function') {
                initMap();
                console.log('Fonction initMap appelée');
                
                // Vérifier les données des bornes
                console.log('Données des bornes:', JSON.stringify(stationData));
            } else {
                console.error('La fonction initMap n\'est pas disponible');
            }
        }, 500);
    ");
        }

        private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url == null) return;

            Debug.WriteLine($"WebView navigating to: {e.Url}");

            // Intercepter les messages du pont JavaScript
            if (e.Url.StartsWith("bridge://"))
            {
                e.Cancel = true;
                string messageJson = Uri.UnescapeDataString(e.Url.Substring(9));
                ProcessBridgeMessage(messageJson);
            }
        }

        // Remplacer la méthode ProcessBridgeMessage existante par celle-ci
        private void ProcessBridgeMessage(string messageJson)
        {
            try
            {
                // Analyser le message JSON
                var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(messageJson);
                if (message != null && message.ContainsKey("action"))
                {
                    string? action = message["action"]?.ToString();
                    if (action == null) return;

                    switch (action)
                    {
                        case "mapReady":
                            Debug.WriteLine("Carte prête");
                            break;
                        case "error":
                            if (message.ContainsKey("data") && message["data"] != null)
                            {
                                var dataObj = message["data"];
                                if (dataObj != null)
                                {
                                    string? dataStr = dataObj.ToString();
                                    if (dataStr != null)
                                    {
                                        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(dataStr ?? "{}");
                                        if (data != null && data.ContainsKey("message") && data.ContainsKey("source") && data.ContainsKey("line"))
                                        {
                                            Debug.WriteLine($"Erreur JavaScript: {data["message"]} à {data["source"]}:{data["line"]}");
                                        }
                                    }
                                }
                            }
                            break;
                        case "stationSelected":
                            if (message.ContainsKey("data") && message["data"] != null)
                            {
                                var dataObj = message["data"];
                                if (dataObj != null)
                                {
                                    string? dataStr = dataObj.ToString();
                                    if (dataStr != null)
                                    {
                                        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(dataStr ?? "{}");
                                        if (data != null && data.ContainsKey("id"))
                                        {
                                            int stationId = Convert.ToInt32(data["id"]);
                                            SelectStation(stationId);
                                        }
                                    }
                                }
                            }
                            break;
                        case "navigate":
                            if (message.ContainsKey("data") && message["data"] != null)
                            {
                                var dataObj = message["data"];
                                if (dataObj != null)
                                {
                                    string? dataStr = dataObj.ToString();
                                    if (dataStr != null)
                                    {
                                        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(dataStr ?? "{}");
                                        if (data != null && data.ContainsKey("id") && data.ContainsKey("address") &&
                                            data.ContainsKey("lat") && data.ContainsKey("lng"))
                                        {
                                            int stationId = Convert.ToInt32(data["id"]);
                                            string address = data["address"].ToString() ?? "";
                                            double lat = Convert.ToDouble(data["lat"]);
                                            double lng = Convert.ToDouble(data["lng"]);

                                            // Appeler la méthode de navigation sur le thread UI
                                            MainThread.BeginInvokeOnMainThread(() => {
                                                NavigateToStation(stationId, address, lat, lng);
                                            });
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du traitement du message du pont: {ex.Message}");
            }
        }

        private async void NavigateToStation(int stationId, string address, double latitude, double longitude)
        {
            try
            {
                // Trouver la borne correspondante
                var borne = _viewModel?.Bornes.FirstOrDefault(b => b.Id == stationId);
                if (borne == null)
                {
                    Debug.WriteLine($"Borne non trouvée avec l'ID: {stationId}");
                    return;
                }

                // Utiliser l'adresse complète formatée
                string formattedAddress = $"{borne.Address}, {borne.PostalCode} {borne.City}";
                Debug.WriteLine($"[DEBUG] Navigation vers: {formattedAddress} ({latitude}, {longitude})");

                // Demander à l'utilisateur quelle application utiliser
                bool useGoogleMaps = await DisplayAlert(
                    "Navigation",
                    $"Naviguer vers {borne.Name}?",
                    "Google Maps",
                    "Maps par défaut");

                // Construire l'URI de navigation
                string navigationUri;

                if (useGoogleMaps)
                {
                    // URI pour Google Maps avec l'adresse formatée
                    navigationUri = $"https://www.google.com/maps/dir/?api=1&destination={Uri.EscapeDataString(formattedAddress)}&travelmode=driving";
                }
                else
                {
                    // URI pour l'application Maps par défaut
                    navigationUri = $"geo:0,0?q={Uri.EscapeDataString(formattedAddress)}";

                    // Pour iOS, utiliser un format différent
                    if (DeviceInfo.Platform == DevicePlatform.iOS)
                    {
                        navigationUri = $"maps://?daddr={Uri.EscapeDataString(formattedAddress)}&dirflg=d";
                    }
                }

                // Ouvrir l'application de navigation
                try
                {
                    await Launcher.OpenAsync(navigationUri);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erreur lors de l'ouverture de l'application de navigation: {ex.Message}");

                    // Si l'ouverture avec l'adresse échoue, essayer avec les coordonnées
                    try
                    {
                        string fallbackUri;
                        if (useGoogleMaps)
                        {
                            fallbackUri = $"https://www.google.com/maps/dir/?api=1&destination={latitude},{longitude}&travelmode=driving";
                        }
                        else
                        {
                            fallbackUri = $"geo:{latitude},{longitude}?q={latitude},{longitude}";
                            if (DeviceInfo.Platform == DevicePlatform.iOS)
                            {
                                fallbackUri = $"maps://?daddr={latitude},{longitude}&dirflg=d";
                            }
                        }

                        await Launcher.OpenAsync(fallbackUri);
                    }
                    catch
                    {
                        await DisplayAlert("Erreur", "Impossible d'ouvrir l'application de navigation.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la navigation vers la borne: {ex.Message}");
                await DisplayAlert("Erreur", "Une erreur s'est produite lors de la navigation.", "OK");
            }
        }


        private string LoadHtmlFromResource(string fileName)
        {
            try
            {
                // Afficher toutes les ressources intégrées pour le débogage
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(MapPage)).Assembly;
                var resources = assembly.GetManifestResourceNames();

                Debug.WriteLine("=== RESSOURCES DISPONIBLES ===");
                foreach (var res in resources)
                {
                    Debug.WriteLine($"- {res}");
                }

                // Essayer plusieurs approches pour trouver le fichier

                // Approche 1: Nom complet avec namespace
                string resourceName = $"SOLARY.Resources.Raw.{fileName}";
                Debug.WriteLine($"Essai 1: Recherche de la ressource: {resourceName}");

                if (resources.Contains(resourceName))
                {
                    Debug.WriteLine($"Ressource trouvée: {resourceName}");
                    using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string content = reader.ReadToEnd();
                                Debug.WriteLine($"Contenu chargé, longueur: {content.Length} caractères");
                                return content;
                            }
                        }
                    }
                }

                // Approche 2: Recherche par correspondance partielle
                Debug.WriteLine("Essai 2: Recherche par correspondance partielle");
                var matchingResource = resources.FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(matchingResource))
                {
                    Debug.WriteLine($"Ressource trouvée avec un nom différent: {matchingResource}");
                    using (Stream? stream = assembly.GetManifestResourceStream(matchingResource))
                    {
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string content = reader.ReadToEnd();
                                Debug.WriteLine($"Contenu chargé, longueur: {content.Length} caractères");
                                return content;
                            }
                        }
                    }
                }

                // Approche 3: Recherche de n'importe quel fichier HTML
                Debug.WriteLine("Essai 3: Recherche de n'importe quel fichier HTML");
                matchingResource = resources.FirstOrDefault(r => r.EndsWith(".html", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(matchingResource))
                {
                    Debug.WriteLine($"Fichier HTML trouvé: {matchingResource}");
                    using (Stream? stream = assembly.GetManifestResourceStream(matchingResource))
                    {
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string content = reader.ReadToEnd();
                                Debug.WriteLine($"Contenu chargé, longueur: {content.Length} caractères");
                                return content;
                            }
                        }
                    }
                }

                // Si tout échoue, retourner une chaîne vide
                Debug.WriteLine("!!! IMPOSSIBLE DE TROUVER LE FICHIER HTML !!!");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERREUR CRITIQUE lors du chargement du fichier HTML: {ex}");
                return string.Empty;
            }
        }

        private void SetupNavigation()
        {
            // Gestionnaire pour l'onglet Accueil
            var accueilTab = this.FindByName<VerticalStackLayout>("AccueilTab");
            if (accueilTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PopAsync();
                };
                accueilTab.GestureRecognizers.Add(tapGesture);
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

            // Gestionnaire pour l'onglet Scan
            var scanTab = this.FindByName<VerticalStackLayout>("ScanTab");
            if (scanTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await DisplayAlert("Scan", "La page Scan n'est pas encore implémentée.", "OK");
                };
                scanTab.GestureRecognizers.Add(tapGesture);
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

        private async void OnFiltersClicked(object? sender, EventArgs e)
        {
            await DisplayAlert("Filtres", "La fonctionnalité de filtres n'est pas encore implémentée.", "OK");
        }

        private void OnMyLocationClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized)
                {
                    DisplayAlert("Erreur", "La carte n'est pas encore initialisée. Veuillez réessayer dans quelques instants.", "OK");
                    return;
                }

                // Appeler la fonction JavaScript pour centrer sur la position de l'utilisateur
                MapWebView?.EvaluateJavaScriptAsync("centerOnUserLocation()");
            }
            catch (Exception ex)
            {
                DisplayAlert("Erreur", $"Erreur lors de la géolocalisation: {ex.Message}", "OK");
            }
        }

        private void OnZoomInClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized) return;
                MapWebView?.EvaluateJavaScriptAsync("zoomIn()");
            }
            catch (Exception ex)
            {
                DisplayAlert("Erreur", $"Erreur lors du zoom: {ex.Message}", "OK");
            }
        }

        private void OnZoomOutClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized) return;
                MapWebView?.EvaluateJavaScriptAsync("zoomOut()");
            }
            catch (Exception ex)
            {
                DisplayAlert("Erreur", $"Erreur lors du zoom: {ex.Message}", "OK");
            }
        }

        private void OnLayersClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized) return;

                // Appeler la fonction JavaScript pour basculer en vue 3D
                MapWebView?.EvaluateJavaScriptAsync("toggle3DView()");
            }
            catch (Exception ex)
            {
                DisplayAlert("Erreur", $"Erreur lors du changement de vue: {ex.Message}", "OK");
            }
        }

        private void OnCenterClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized) return;
                MapWebView?.EvaluateJavaScriptAsync("centerMap()");
            }
            catch (Exception ex)
            {
                DisplayAlert("Erreur", $"Erreur lors du recentrage: {ex.Message}", "OK");
            }
        }

        // Modifier la méthode ZoomToStation pour ajuster le niveau de zoom
        private void ZoomToStation(int stationId)
        {
            try
            {
                if (!_mapInitialized)
                {
                    DisplayAlert("Erreur", "La carte n'est pas encore initialisée. Veuillez réessayer dans quelques instants.", "OK");
                    return;
                }

                Debug.WriteLine($"Zoom to station: {stationId}");

                // Sélectionner la station dans l'interface
                SelectStation(stationId);

                // Appeler la fonction JavaScript pour zoomer sur la station
                MapWebView?.EvaluateJavaScriptAsync($"zoomToStation({stationId})");

                // Faire défiler vers le haut pour voir la carte complètement
                _mainScrollView?.ScrollToAsync(0, 0, true);

                // S'assurer que la carte est suffisamment grande pour afficher la popup
                if (_mapContainer != null)
                {
                    _mapContainer.HeightRequest = 450;
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Erreur", $"Erreur lors du zoom sur la station: {ex.Message}", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Nettoyer les ressources de la WebView
            try
            {
                MapWebView?.EvaluateJavaScriptAsync("map = null;");

                // Forcer le nettoyage du cache
#if ANDROID
                var handler = MapWebView?.Handler as Microsoft.Maui.Handlers.WebViewHandler;
                if (handler != null)
                {
                    var webView = handler.PlatformView as Android.Webkit.WebView;
                    if (webView != null)
                    {
                        webView.ClearCache(true);
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du nettoyage de la WebView: {ex.Message}");
            }
        }
    }

    // Interface JavaScript pour Android
#if ANDROID
    public class WebViewJavaScriptInterface : Java.Lang.Object
    {
        private readonly MapPage _page;

        public WebViewJavaScriptInterface(MapPage page)
        {
            _page = page;
        }

        [Android.Webkit.JavascriptInterface]
        public void ReceiveMessage(string message)
        {
            try
            {
                Debug.WriteLine($"Message reçu du JavaScript: {message}");

                // Traiter le message ici
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                if (data != null && data.ContainsKey("action"))
                {
                    string? action = data["action"]?.ToString();
                    if (action == null) return;

                    // Traiter l'action
                    switch (action)
                    {
                        case "mapReady":
                            Debug.WriteLine("Carte prête");
                            break;
                        case "error":
                            if (data.ContainsKey("data") && data["data"] != null)
                            {
                                string? dataStr = data["data"]?.ToString();
                                if (dataStr != null)
                                {
                                    var errorData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(dataStr);
                                    if (errorData != null && errorData.ContainsKey("message") && errorData.ContainsKey("source") && errorData.ContainsKey("line"))
                                    {
                                        Debug.WriteLine($"Erreur JavaScript: {errorData["message"]} à {errorData["source"]}:{errorData["line"]}");
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du traitement du message JavaScript: {ex.Message}");
            }
        }
    }
#endif
}
