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
using SOLARY.Model;
using SOLARY.Services;
using System.Timers;
using System.Globalization;
using System.Threading.Tasks;

namespace SOLARY.Views
{
    public partial class MapPage : ContentPage
    {
        private MapViewModel? _viewModel;
        private bool _mapInitialized = false;
        private bool _mapLoading = false;
        private VerticalStackLayout? _stationListLayout;
        private int _selectedStationId = -1;
        private Dictionary<int, Border> _stationCards = new Dictionary<int, Border>();
        private Dictionary<int, VerticalStackLayout> _casierContainers = new Dictionary<int, VerticalStackLayout>();

        // Variables de classe existantes
        private ScrollView? _mainScrollView;
        private Grid? _mapContainer;
        private double _lastScrollY = 0;

        // Services
        private readonly ICasierService _casierService;
        private readonly IUserService _userService;

        // Variables pour la mise à jour en temps réel
        private System.Timers.Timer? _casierUpdateTimer;
        private bool _isUpdatingCasiers = false;

        // Utilisateur actuel
        private User? _currentUser;

        // Variables pour l'optimisation mobile
        private bool _isMobileDevice = false;
        private int _mapInitRetryCount = 0;
        private const int MAX_INIT_RETRIES = 3;
        private TaskCompletionSource<bool>? _mapInitializationTcs;

        private WebView? GetMapWebView() => this.FindByName<WebView>("MapWebView");

        public MapPage()
        {
            InitializeComponent();

            // Détecter si c'est un appareil mobile
            _isMobileDevice = DeviceInfo.Platform == DevicePlatform.Android ||
                             DeviceInfo.Platform == DevicePlatform.iOS;

            // Initialiser les services
            _casierService = new CasierService();
            _userService = new UserService();

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

            // Initialiser la carte après le chargement de la page avec optimisations mobiles
            this.Loaded += OnPageLoaded;

            // Ajouter un gestionnaire pour le scroll principal
            var mainScrollView = this.FindByName<ScrollView>("MainScrollView");
            if (mainScrollView != null)
            {
                mainScrollView.Scrolled += OnMainScrolled;
            }

            // Initialiser l'utilisateur actuel
            _ = InitializeCurrentUser();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Configurer le plein écran pour MapPage
            ConfigureFullScreen();

            // Optimisations spécifiques pour mobile
            if (_isMobileDevice)
            {
                OptimizeForMobile();
            }
        }

        private void OptimizeForMobile()
        {
            try
            {
                var mapWebView = GetMapWebView();
                if (mapWebView != null)
                {
                    // Optimisations WebView pour mobile
                    mapWebView.BackgroundColor = Colors.Black;

#if ANDROID
                    // Optimisations Android spécifiques - SUPPRESSION DES MÉTHODES OBSOLÈTES
                    var handler = mapWebView.Handler as Microsoft.Maui.Handlers.WebViewHandler;
                    if (handler?.PlatformView is Android.Webkit.WebView androidWebView)
                    {
                        // Optimisations de base pour Android
                        androidWebView.Settings.JavaScriptEnabled = true;
                        androidWebView.Settings.DomStorageEnabled = true;
                        androidWebView.Settings.DatabaseEnabled = true;
                        androidWebView.Settings.CacheMode = Android.Webkit.CacheModes.CacheElseNetwork;

                        // Optimisations tactiles
                        androidWebView.Settings.SetSupportZoom(true);
                        androidWebView.Settings.BuiltInZoomControls = false;
                        androidWebView.Settings.DisplayZoomControls = false;
                        androidWebView.Settings.LoadWithOverviewMode = true;
                        androidWebView.Settings.UseWideViewPort = true;

                        // Désactiver les animations coûteuses sur mobile
                        androidWebView.SetLayerType(Android.Views.LayerType.Hardware, null);
                    }
#endif

#if IOS
                    // Optimisations iOS spécifiques
                    var handler = mapWebView.Handler as Microsoft.Maui.Handlers.WebViewHandler;
                    if (handler?.PlatformView is WebKit.WKWebView iosWebView)
                    {
                        // Configuration pour iOS
                        iosWebView.Configuration.AllowsInlineMediaPlayback = true;
                        iosWebView.Configuration.MediaTypesRequiringUserActionForPlayback = WebKit.WKAudiovisualMediaTypes.None;
                        
                        // Optimisations de performance
                        iosWebView.Configuration.Preferences.JavaScriptEnabled = true;
                        iosWebView.Configuration.Preferences.JavaScriptCanOpenWindowsAutomatically = false;
                        
                        // Optimisations de mémoire
                        iosWebView.Configuration.ProcessPool = new WebKit.WKProcessPool();
                    }
#endif
                }

                Debug.WriteLine("[MapPage] Optimisations mobiles appliquées");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors des optimisations mobiles: {ex.Message}");
            }
        }

        private void ConfigureFullScreen()
        {
            try
            {
                // Pour Android
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
#if ANDROID
                    var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                    if (activity?.Window != null)
                    {
                        // Rendre la barre d'état transparente
                        activity.Window.SetStatusBarColor(Android.Graphics.Color.Transparent);

                        // Configuration moderne pour Android 11+
                        if (OperatingSystem.IsAndroidVersionAtLeast(30))
                        {
                            ConfigureModernFullScreen(activity);
                        }
                        else
                        {
                            ConfigureLegacyFullScreen(activity);
                        }
                    }
#endif
                }
                // Pour iOS, la configuration est dans Info.plist
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration plein écran: {ex.Message}");
            }
        }

#if ANDROID
        private static void ConfigureModernFullScreen(Android.App.Activity activity)
        {
            try
            {
                var window = activity.Window;
                if (window != null)
                {
                    // Utiliser la réflexion pour WindowInsetsController
                    var insetsControllerProperty = window.GetType().GetProperty("InsetsController");
                    if (insetsControllerProperty != null)
                    {
                        var insetsController = insetsControllerProperty.GetValue(window);
                        if (insetsController != null)
                        {
                            var insetsControllerType = insetsController.GetType();

                            // Masquer la barre d'état
                            var hideMethod = insetsControllerType.GetMethod("Hide");
                            if (hideMethod != null)
                            {
                                hideMethod.Invoke(insetsController, new object[] { 1 }); // 1 = StatusBars
                            }

                            // Définir le comportement
                            var setBehaviorProperty = insetsControllerType.GetProperty("SystemBarsBehavior");
                            if (setBehaviorProperty != null)
                            {
                                setBehaviorProperty.SetValue(insetsController, 1); // BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                            }
                        }
                    }

                    // SetDecorFitsSystemWindows
                    var setDecorFitsMethod = window.GetType().GetMethod("SetDecorFitsSystemWindows");
                    if (setDecorFitsMethod != null)
                    {
                        setDecorFitsMethod.Invoke(window, new object[] { false });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur avec WindowInsetsController: {ex.Message}");
                ConfigureLegacyFullScreen(activity);
            }
        }

        private static void ConfigureLegacyFullScreen(Android.App.Activity activity)
        {
            try
            {
                var window = activity.Window;
                if (window?.DecorView != null)
                {
#pragma warning disable CA1422 // Validate platform compatibility
#pragma warning disable CS0618 // Type or member is obsolete
                    window.DecorView.SystemUiFlags = (Android.Views.SystemUiFlags)(
                        Android.Views.SystemUiFlags.LayoutFullscreen |
                        Android.Views.SystemUiFlags.LayoutStable |
                        Android.Views.SystemUiFlags.Fullscreen |
                        Android.Views.SystemUiFlags.ImmersiveSticky |
                        Android.Views.SystemUiFlags.HideNavigation |
                        Android.Views.SystemUiFlags.LayoutHideNavigation);
#pragma warning restore CS0618
#pragma warning restore CA1422
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur avec SystemUiFlags: {ex.Message}");
            }
        }
#endif

        private async Task InitializeCurrentUser()
        {
            try
            {
                if (SessionService.IsLoggedIn)
                {
                    int userId = SessionService.CurrentUserId ?? 0;
                    if (userId == 0)
                    {
                        Debug.WriteLine("[MapPage] Aucun utilisateur connecté");
                        await DisplayAlert("Connexion requise", "Vous devez être connecté pour accéder à cette page.", "OK");
                        await Shell.Current.GoToAsync("//LoginPage");
                        return;
                    }
                    if (userId > 0)
                    {
                        _currentUser = await _userService.GetUserById(userId);
                        Debug.WriteLine($"[MapPage] Utilisateur connecté: {_currentUser?.Email ?? "Inconnu"} (ID: {userId})");
                    }
                }
                else
                {
                    Debug.WriteLine("[MapPage] Aucun utilisateur connecté");
                    // Rediriger vers la page de connexion
                    await DisplayAlert("Connexion requise", "Vous devez être connecté pour accéder à cette page.", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors de l'initialisation de l'utilisateur: {ex.Message}");
                await DisplayAlert("Erreur", "Impossible de charger les informations utilisateur.", "OK");
            }
        }

        private void OnMainScrolled(object? sender, ScrolledEventArgs e)
        {
            if (_mapContainer == null) return;

            if (Math.Abs(e.ScrollY - _lastScrollY) < 1)
            {
                return;
            }

            bool isScrollingDown = e.ScrollY > _lastScrollY;
            _lastScrollY = e.ScrollY;

            double minHeight = _isMobileDevice ? 120 : 100; // Plus de hauteur minimale sur mobile
            double maxHeight = _isMobileDevice ? 400 : 490; // Hauteur adaptée pour mobile
            double currentHeight = _mapContainer.HeightRequest;

            var searchBar = this.FindByName<SearchBar>("SearchBar");
            if (searchBar != null)
            {
                if (e.ScrollY < 50)
                {
                    searchBar.IsVisible = true;
                    searchBar.Opacity = 1;
                }
                else if (isScrollingDown && e.ScrollY > 50)
                {
                    double opacity = Math.Max(0, 1 - ((e.ScrollY - 50) / 100));
                    searchBar.Opacity = opacity;
                    searchBar.IsVisible = opacity > 0.1;
                }
            }

            // Animations plus fluides sur mobile
            double animationFactor = _isMobileDevice ? 0.3 : 0.5;

            if (isScrollingDown && e.ScrollY > 50)
            {
                double newHeight = Math.Max(minHeight, maxHeight - (e.ScrollY - 50) * animationFactor);
                if (Math.Abs(newHeight - currentHeight) > 5)
                {
                    _mapContainer.HeightRequest = newHeight;
                }
            }
            else if (!isScrollingDown && e.ScrollY < 200)
            {
                double newHeight = Math.Min(maxHeight, minHeight + (200 - e.ScrollY) * (animationFactor * 2));
                if (Math.Abs(newHeight - currentHeight) > 5)
                {
                    _mapContainer.HeightRequest = newHeight;
                }
            }

            if (e.ScrollY < 10)
            {
                _mapContainer.HeightRequest = maxHeight;
            }

            if (e.ScrollY > 1000)
            {
                _mapContainer.HeightRequest = minHeight;
            }
        }

        private async void OnPageLoaded(object? sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("[MapPage] Page chargée, début de l'initialisation");

                // Initialiser la carte avec retry pour mobile
                await InitializeMapWithRetry();

                _mainScrollView = this.FindByName<ScrollView>("MainScrollView");
                _mapContainer = this.FindByName<Grid>("MapContainer");

                var mapWebView = this.FindByName<WebView>("MapWebView");
                if (mapWebView != null)
                {
                    mapWebView.BackgroundColor = Colors.Black;
                }

                // Charger les données des stations
                await LoadStationData();

                // Démarrer les mises à jour en temps réel (moins fréquentes sur mobile)
                StartRealTimeUpdates();

                Debug.WriteLine("[MapPage] Initialisation de la page terminée");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors du chargement de la page: {ex.Message}");
                await DisplayAlert("Erreur", "Erreur lors du chargement de la carte. Veuillez réessayer.", "OK");
            }
        }

        // 2. Optimiser la méthode InitializeMapWithRetry pour accélérer l'initialisation
        private async Task InitializeMapWithRetry()
        {
            _mapInitRetryCount = 0;
            _mapInitializationTcs = new TaskCompletionSource<bool>();

            // Réduire le nombre de tentatives et accélérer le processus
            const int FAST_INIT_RETRIES = 2;

            while (_mapInitRetryCount < FAST_INIT_RETRIES && !_mapInitialized)
            {
                try
                {
                    Debug.WriteLine($"[MapPage] Tentative d'initialisation rapide de la carte #{_mapInitRetryCount + 1}");

                    // Initialiser la carte avec un timeout plus court
                    await InitializeMapAsync();

                    // Attendre l'initialisation avec timeout réduit
                    var timeoutTask = Task.Delay(_isMobileDevice ? 5000 : 3000); // Réduit de 10000/5000 à 5000/3000
                    var completedTask = await Task.WhenAny(_mapInitializationTcs.Task, timeoutTask);

                    if (completedTask == _mapInitializationTcs.Task && _mapInitializationTcs.Task.Result)
                    {
                        Debug.WriteLine("[MapPage] Carte initialisée avec succès");
                        break;
                    }
                    else
                    {
                        Debug.WriteLine($"[MapPage] Timeout ou échec de l'initialisation, tentative {_mapInitRetryCount + 1}");
                        _mapInitRetryCount++;

                        if (_mapInitRetryCount < FAST_INIT_RETRIES)
                        {
                            await Task.Delay(500); // Réduit de 2000/1000 à 500ms
                            _mapInitializationTcs = new TaskCompletionSource<bool>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MapPage] Erreur lors de la tentative {_mapInitRetryCount + 1}: {ex.Message}");
                    _mapInitRetryCount++;

                    if (_mapInitRetryCount < FAST_INIT_RETRIES)
                    {
                        await Task.Delay(500);
                        _mapInitializationTcs = new TaskCompletionSource<bool>();
                    }
                }
            }

            // Si l'initialisation a échoué, afficher un message mais continuer à essayer en arrière-plan
            if (!_mapInitialized)
            {
                Debug.WriteLine("[MapPage] Échec de l'initialisation rapide, continuant en arrière-plan");

                // Lancer une initialisation en arrière-plan sans bloquer l'interface
                _ = Task.Run(async () => {
                    try
                    {
                        await Task.Delay(1000);
                        await MainThread.InvokeOnMainThreadAsync(async () => {
                            await InitializeMapAsync();
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MapPage] Erreur lors de l'initialisation en arrière-plan: {ex.Message}");
                    }
                });
            }
        }

        private async Task InitializeMapAsync()
        {
            if (_mapLoading) return;

            try
            {
                _mapLoading = true;
                Debug.WriteLine("[MapPage] Début de l'initialisation de la carte");

                string htmlContent = await LoadHtmlFromResourceAsync("map.html");

                if (string.IsNullOrWhiteSpace(htmlContent))
                {
                    Debug.WriteLine("ERREUR: Contenu HTML non trouvé dans les ressources");
                    throw new Exception("Fichier map.html non trouvé");
                }

                var mapWebView = GetMapWebView();
                if (mapWebView == null)
                {
                    throw new Exception("WebView non trouvée");
                }

                // Optimisations spécifiques pour mobile
                if (_isMobileDevice)
                {
                    await ConfigureMobileWebView(mapWebView);
                }

                // Préparer les données des bornes
                string bornesJson = "[]";
                if (_viewModel != null)
                {
                    // Attendre que les bornes soient chargées
                    int waitCount = 0;
                    while (_viewModel.Bornes.Count == 0 && waitCount < 50) // 5 secondes max
                    {
                        await Task.Delay(100);
                        waitCount++;
                    }

                    bornesJson = _viewModel.GetBornesJson();
                    Debug.WriteLine($"[MapPage] Données des bornes générées: {bornesJson.Length} caractères");
                }

                // Injecter les données dans le HTML
                htmlContent = htmlContent.Replace("var stationData = [];", $"var stationData = {bornesJson};");

                // Ajouter des optimisations pour mobile dans le HTML
                if (_isMobileDevice)
                {
                    htmlContent = InjectMobileOptimizations(htmlContent);
                }

                // Ajouter un timestamp pour éviter le cache
                string timestamp = DateTime.Now.Ticks.ToString();
                htmlContent = htmlContent.Replace("</head>",
                    $"<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\" />" +
                    $"<meta http-equiv=\"Pragma\" content=\"no-cache\" />" +
                    $"<meta http-equiv=\"Expires\" content=\"0\" />" +
                    $"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no\" />" +
                    $"<script>console.log('Version: {timestamp}');</script></head>");

                Debug.WriteLine($"[MapPage] Chargement de la carte avec timestamp: {timestamp}");

                // Configurer les événements WebView
                mapWebView.Navigated -= OnWebViewNavigated; // Éviter les doublons
                mapWebView.Navigated += OnWebViewNavigated;
                mapWebView.Navigating -= OnWebViewNavigating;
                mapWebView.Navigating += OnWebViewNavigating;

                // Charger le contenu
                mapWebView.Source = new HtmlWebViewSource { Html = htmlContent };
                mapWebView.IsVisible = true;

                Debug.WriteLine("[MapPage] Contenu HTML chargé dans la WebView");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors de l'initialisation: {ex.Message}");
                throw;
            }
            finally
            {
                _mapLoading = false;
            }
        }

        // 3. Améliorer la fluidité de la carte sur mobile - CORRECTION DE LA SIGNATURE
        private async Task ConfigureMobileWebView(WebView webView)
        {
#if ANDROID
            var handler = webView.Handler as Microsoft.Maui.Handlers.WebViewHandler;
            if (handler?.PlatformView is Android.Webkit.WebView androidWebView)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Configuration optimisée pour Android avec focus sur la performance
                    androidWebView.Settings.JavaScriptEnabled = true;
                    androidWebView.Settings.DomStorageEnabled = true;
                    androidWebView.Settings.AllowFileAccess = true;
                    androidWebView.Settings.AllowContentAccess = true;
                    androidWebView.Settings.AllowFileAccessFromFileURLs = true;
                    androidWebView.Settings.AllowUniversalAccessFromFileURLs = true;

                    // Optimisations de performance critiques
                    androidWebView.Settings.CacheMode = Android.Webkit.CacheModes.CacheElseNetwork;
                    androidWebView.Settings.DatabaseEnabled = true;
                    androidWebView.Settings.LoadWithOverviewMode = true;
                    androidWebView.Settings.UseWideViewPort = true;

                    // Optimisations tactiles pour une meilleure fluidité
                    androidWebView.Settings.SetSupportZoom(true);
                    androidWebView.Settings.BuiltInZoomControls = true;
                    androidWebView.Settings.DisplayZoomControls = false;

                    // Optimisations de rendu hardware
                    androidWebView.SetLayerType(Android.Views.LayerType.Hardware, null);

                    // Nettoyer le cache pour éviter les problèmes d'affichage
                    androidWebView.ClearCache(true);
                    androidWebView.AddJavascriptInterface(new WebViewJavaScriptInterface(this), "jsBridge");

                    Debug.WriteLine("[MapPage] WebView Android configurée avec optimisations de performance");
                });
            }
#endif

#if IOS || MACCATALYST
var handler = webView.Handler as Microsoft.Maui.Handlers.WebViewHandler;
if (handler?.PlatformView is WebKit.WKWebView iosWebView)
{
    await MainThread.InvokeOnMainThreadAsync(() =>
    {
        // Nettoyer le cache
        var dataTypes = WebKit.WKWebsiteDataStore.AllWebsiteDataTypes;
        WebKit.WKWebsiteDataStore.DefaultDataStore.RemoveDataOfTypes(
            dataTypes,
            Foundation.NSDate.FromTimeIntervalSinceNow(-604800), // Nettoyer seulement le cache d'une semaine
            () => Debug.WriteLine("Cache iOS nettoyé")
        );

        // Optimisations de performance pour iOS
        var config = iosWebView.Configuration;
        config.AllowsInlineMediaPlayback = true;
        config.MediaTypesRequiringUserActionForPlayback = WebKit.WKAudiovisualMediaTypes.None;
        
        // CORRECTION : Utiliser la nouvelle API pour iOS 14+
        if (OperatingSystem.IsIOSVersionAtLeast(14) || OperatingSystem.IsMacCatalystVersionAtLeast(14))
        {
            // Utiliser la nouvelle API pour iOS 14+
            config.DefaultWebpagePreferences.AllowsContentJavaScript = true;
        }
        else
        {
            // Utiliser l'ancienne API pour les versions antérieures
#pragma warning disable CA1416 // Validate platform compatibility
#pragma warning disable CS0618 // Type or member is obsolete
            config.Preferences.JavaScriptEnabled = true;
#pragma warning restore CS0618
#pragma warning restore CA1416
        }
        
        // Optimisations pour le défilement fluide
        iosWebView.ScrollView.Bounces = true;
        
        // Script pour l'interface JavaScript avec optimisations tactiles
        var scriptContent = @"
    window.webkit.messageHandlers.jsBridge = { 
        postMessage: function(message) { 
            window.location.href = 'bridge://' + encodeURIComponent(JSON.stringify(message)); 
        } 
    };
    
    // Optimisations tactiles pour iOS
    document.addEventListener('touchstart', function(e) {
        if (e.touches.length > 1) {
            e.preventDefault();
        }
    }, { passive: true });
    
    document.addEventListener('touchmove', function(e) {
        if (e.touches.length > 1) {
            e.preventDefault();
        }
    }, { passive: true });
    
    // Optimisations de défilement
    document.body.style.webkitOverflowScrolling = 'touch';
";

        var script = new WebKit.WKUserScript(
            new Foundation.NSString(scriptContent),
            WebKit.WKUserScriptInjectionTime.AtDocumentStart,
            false
        );

        iosWebView.Configuration.UserContentController.AddUserScript(script);
        
        Debug.WriteLine("[MapPage] WebView iOS configurée avec optimisations de performance");
    });
}
#endif
        }

        // 4. Améliorer les optimisations mobiles injectées dans le HTML
        private string InjectMobileOptimizations(string htmlContent)
        {
            // Ajouter des optimisations CSS et JavaScript avancées pour mobile
            var mobileOptimizations = @"
        <style>
            /* Optimisations mobiles avancées */
            * {
                -webkit-tap-highlight-color: transparent;
                -webkit-touch-callout: none;
                -webkit-user-select: none;
                user-select: none;
            }
            
            body {
                -webkit-overflow-scrolling: touch;
                overflow: hidden;
                position: fixed;
                width: 100%;
                height: 100%;
                margin: 0;
                padding: 0;
            }
            
            .leaflet-container {
                -webkit-transform: translate3d(0,0,0);
                transform: translate3d(0,0,0);
                width: 100% !important;
                height: 100% !important;
                background: #121824;
            }
            
            .station-marker {
                will-change: transform;
                -webkit-transform: translate3d(0,0,0);
                transform: translate3d(0,0,0);
                z-index: 500 !important;
            }
            
            .leaflet-popup {
                -webkit-transform: translate3d(0,0,0);
                transform: translate3d(0,0,0);
                z-index: 600 !important;
            }
            
            /* Optimisations pour les boutons de la carte */
            .leaflet-control-zoom a {
                width: 36px !important;
                height: 36px !important;
                line-height: 36px !important;
                font-size: 18px !important;
            }
            
            /* Optimisations pour les popups */
            .leaflet-popup-content {
                margin: 14px !important;
                font-size: 14px !important;
            }
            
            /* Optimisations pour le défilement fluide */
            .leaflet-fade-anim .leaflet-tile,
            .leaflet-zoom-anim .leaflet-zoom-animated {
                will-change: transform, opacity;
                -webkit-backface-visibility: hidden;
                backface-visibility: hidden;
            }
        </style>
        
        <script>
            // Optimisations JavaScript avancées pour mobile
            window.isMobile = true;
            window.devicePixelRatio = window.devicePixelRatio || 1;
            
            // Désactiver le zoom par pincement au niveau du document
            document.addEventListener('gesturestart', function (e) {
                e.preventDefault();
            }, {passive: false});
            
            document.addEventListener('gesturechange', function (e) {
                e.preventDefault();
            }, {passive: false});
            
            document.addEventListener('gestureend', function (e) {
                e.preventDefault();
            }, {passive: false});
            
            // Optimiser les performances de rendu
            if (window.requestIdleCallback) {
                window.requestIdleCallback(function() {
                    console.log('Optimisations mobiles avancées appliquées');
                });
            }
            
            // Optimisations pour Leaflet
            document.addEventListener('DOMContentLoaded', function() {
                if (typeof L !== 'undefined' && L.Map) {
                    // Optimiser les paramètres de la carte Leaflet
                    L.Map.mergeOptions({
                        preferCanvas: true,
                        renderer: L.canvas(),
                        fadeAnimation: false,
                        markerZoomAnimation: true,
                        zoomAnimation: true,
                        zoomSnap: 0.5,
                        zoomDelta: 0.5,
                        wheelPxPerZoomLevel: 120,
                        tap: true,
                        tapTolerance: 15,
                        bounceAtZoomLimits: false
                    });
                    
                    console.log('Optimisations Leaflet appliquées');
                }
            });
            
            // Améliorer la réactivité tactile
            document.addEventListener('touchstart', function() {}, {passive: true});
            document.addEventListener('touchmove', function() {}, {passive: true});
            document.addEventListener('touchend', function() {}, {passive: true});
            document.addEventListener('touchcancel', function() {}, {passive: true});
        </script>
    ";

            return htmlContent.Replace("</head>", mobileOptimizations + "</head>");
        }

        private async Task<string> LoadHtmlFromResourceAsync(string fileName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var assembly = IntrospectionExtensions.GetTypeInfo(typeof(MapPage)).Assembly;
                    var resources = assembly.GetManifestResourceNames();

                    Debug.WriteLine("=== RESSOURCES DISPONIBLES ===");
                    foreach (var res in resources)
                    {
                        Debug.WriteLine($"- {res}");
                    }

                    string resourceName = $"SOLARY.Resources.Raw.{fileName}";
                    Debug.WriteLine($"Recherche de la ressource: {resourceName}");

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

                    // Recherche par correspondance partielle
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

                    Debug.WriteLine("!!! IMPOSSIBLE DE TROUVER LE FICHIER HTML !!!");
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERREUR lors du chargement du fichier HTML: {ex}");
                    return string.Empty;
                }
            });
        }

        private async Task LoadStationData()
        {
            try
            {
                if (_stationListLayout != null && _viewModel != null)
                {
                    // Attendre que les bornes soient chargées
                    int waitCount = 0;
                    while (_viewModel.Bornes.Count == 0 && waitCount < 100) // 10 secondes max
                    {
                        await Task.Delay(100);
                        waitCount++;
                    }

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        _stationListLayout.Clear();
                        _stationCards.Clear();
                        _casierContainers.Clear();

                        // Charger les casiers pour chaque borne
                        foreach (var borne in _viewModel.Bornes)
                        {
                            try
                            {
                                var casiers = await _casierService.GetCasiersByBorneIdAsync(borne.BorneId);
                                borne.Casiers = casiers ?? new List<Casier>();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[MapPage] Erreur lors du chargement des casiers pour la borne {borne.BorneId}: {ex.Message}");
                                borne.Casiers = new List<Casier>();
                            }
                        }

                        var sortedBornes = _viewModel.Bornes.OrderByDescending(b => b.IsAvailable).ToList();

                        foreach (var borne in sortedBornes)
                        {
                            var stationCard = CreateStationCard(borne);
                            _stationListLayout.Add(stationCard);
                            _stationCards[borne.Id] = stationCard;
                        }

                        Debug.WriteLine($"[MapPage] {sortedBornes.Count} stations chargées dans l'interface");
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors du chargement des données des stations: {ex.Message}");
            }
        }

        // 5. Optimiser la méthode OnWebViewNavigated pour accélérer l'initialisation
        private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"[MapPage] WebView navigated: {e.Result}");

                if (e.Result == WebNavigationResult.Success)
                {
                    var mapWebView = GetMapWebView();
                    if (mapWebView != null)
                    {
                        // Réduire le délai d'initialisation
                        int initDelay = _isMobileDevice ? 500 : 200; // Réduit de 1500/500 à 500/200

                        var timer = Application.Current?.Dispatcher.CreateTimer();
                        if (timer != null)
                        {
                            timer.Interval = TimeSpan.FromMilliseconds(initDelay);
                            timer.Tick += async (s, e) =>
                            {
                                timer.Stop();
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    try
                                    {
                                        // Script d'initialisation optimisé
                                        await mapWebView.EvaluateJavaScriptAsync(@"
                                            console.log('Initialisation rapide de la carte...');
                                            if (typeof initMap === 'function') {
                                                // Initialiser la carte immédiatement
                                                initMap();
                                                console.log('Fonction initMap appelée');
                                        
                                                // Notifier que la carte est prête immédiatement
                                                if (window.jsBridge) {
                                                    window.jsBridge.postMessage(JSON.stringify({
                                                        action: 'mapReady',
                                                        data: { initialized: true }
                                                    }));
                                                }
                                        
                                                // Optimisations supplémentaires après chargement
                                                setTimeout(function() {
                                                    if (typeof map !== 'undefined' && map) {
                                                        map.invalidateSize();
                                                        console.log('Taille de carte invalidée');
                                                
                                                        // Optimiser le rendu
                                                        if (L && L.DomUtil) {
                                                            L.DomUtil.addClass(map._container, 'leaflet-gpu');
                                                        }
                                                    }
                                                }, 300);
                                            } else {
                                                console.error('La fonction initMap n\'est pas disponible');
                                                if (window.jsBridge) {
                                                    window.jsBridge.postMessage(JSON.stringify({
                                                        action: 'error',
                                                        data: { message: 'initMap not found' }
                                                    }));
                                                }
                                            }
                                        ");

                                        Debug.WriteLine("[MapPage] Script d'initialisation rapide exécuté");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[MapPage] Erreur lors de l'exécution du script: {ex.Message}");
                                    }
                                });
                            };
                            timer.Start();
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"[MapPage] Échec de la navigation WebView: {e.Result}");
                    _mapInitializationTcs?.TrySetResult(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur dans OnWebViewNavigated: {ex.Message}");
                _mapInitializationTcs?.TrySetResult(false);
            }
        }

        // AJOUT DE LA MÉTHODE OnWebViewNavigating MANQUANTE
        private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url == null) return;

            Debug.WriteLine($"[MapPage] WebView navigating to: {e.Url}");

            if (e.Url.StartsWith("bridge://"))
            {
                e.Cancel = true;
                string messageJson = Uri.UnescapeDataString(e.Url.Substring(9));
                _ = ProcessBridgeMessage(messageJson); // CORRECTION CS4014
            }
        }

        // 6. Ajouter une méthode pour forcer le rafraîchissement de la carte
        private async Task ForceMapRefresh()
        {
            try
            {
                var mapWebView = GetMapWebView();
                if (mapWebView != null && _mapInitialized)
                {
                    await mapWebView.EvaluateJavaScriptAsync(@"
                if (typeof map !== 'undefined' && map) {
                    // Forcer un rafraîchissement complet de la carte
                    map.invalidateSize({pan: false});
                    
                    // Optimiser le rendu
                    if (typeof L !== 'undefined') {
                        // Réinitialiser les tuiles pour un affichage plus net
                        map.eachLayer(function(layer) {
                            if (layer instanceof L.TileLayer) {
                                layer.redraw();
                            }
                        });
                    }
                    
                    console.log('Carte rafraîchie avec succès');
                }
            ");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors du rafraîchissement forcé de la carte: {ex.Message}");
            }
        }

        // 7. Modifier la méthode ProcessBridgeMessage pour améliorer la gestion de l'initialisation
        private async Task ProcessBridgeMessage(string messageJson)
        {
            try
            {
                var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(messageJson);
                if (message != null && message.ContainsKey("action"))
                {
                    string? action = message["action"]?.ToString();
                    if (action == null) return;

                    Debug.WriteLine($"[MapPage] Message reçu: {action}");

                    switch (action)
                    {
                        case "mapReady":
                            Debug.WriteLine("[MapPage] Carte prête - initialisation réussie");
                            _mapInitialized = true;
                            _mapInitializationTcs?.TrySetResult(true);

                            // Forcer un rafraîchissement immédiat de la carte
                            await ForceMapRefresh();

                            // Planifier un autre rafraîchissement après un court délai
                            var refreshTimer = Application.Current?.Dispatcher.CreateTimer();
                            if (refreshTimer != null)
                            {
                                refreshTimer.Interval = TimeSpan.FromMilliseconds(300);
                                refreshTimer.Tick += async (s, e) =>
                                {
                                    refreshTimer.Stop();
                                    await ForceMapRefresh();
                                };
                                refreshTimer.Start();
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
                                            await MainThread.InvokeOnMainThreadAsync(() => SelectStation(stationId));
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

                                            await MainThread.InvokeOnMainThreadAsync(async () => {
                                                await NavigateToStation(stationId, address, lat, lng);
                                            });
                                        }
                                    }
                                }
                            }
                            break;

                        case "error":
                            Debug.WriteLine("[MapPage] Erreur reçue de la carte JavaScript");
                            _mapInitializationTcs?.TrySetResult(false);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors du traitement du message du pont: {ex.Message}");
            }
        }

        // NOUVELLE méthode CreateStationCard avec support des casiers
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

            // Conteneur principal vertical
            var mainContainer = new VerticalStackLayout
            {
                Spacing = 0
            };

            // Créer le contenu principal de la carte (existant)
            var contentGrid = CreateMainStationContent(borne);
            mainContainer.Add(contentGrid);

            // Ajouter le conteneur des casiers (initialement masqué)
            if (borne.HasCasiers)
            {
                var casierContainer = CreateCasierContainer(borne);
                _casierContainers[borne.BorneId] = casierContainer;
                mainContainer.Add(casierContainer);
            }

            stationBorder.Content = mainContainer;

            // Ajouter un effet visuel pour le survol
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => {
                ToggleStationSelection(borne.Id);
            };
            stationBorder.GestureRecognizers.Add(tapGesture);

            return stationBorder;
        }

        private Grid CreateMainStationContent(BorneModel borne)
        {
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
                batteryColor = Color.FromArgb("#F44336");
            }
            else if (borne.IsAvailable)
            {
                batteryImageSource = "battery_yellow.png";
                batteryColor = Color.FromArgb("#FFD602");
            }
            else
            {
                batteryImageSource = "battery_grey.png";
                batteryColor = Color.FromArgb("#BBBBBB");
            }

            var batteryContainer = new Frame
            {
                BackgroundColor = Colors.Black,
                CornerRadius = 20,
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

            var nameLabel = new Label
            {
                Text = borne.Name,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            infoStack.Add(nameLabel);

            var addressLabel = new Label
            {
                Text = borne.Address,
                FontSize = 14,
                TextColor = Color.FromArgb("#BBBBBB"),
                LineBreakMode = LineBreakMode.TailTruncation
            };
            infoStack.Add(addressLabel);

            // Distance et info casiers
            var infoRow = new HorizontalStackLayout
            {
                Spacing = 15,
                Margin = new Thickness(0, 6, 0, 0)
            };

            // Distance
            var distanceStack = new HorizontalStackLayout
            {
                Spacing = 6
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
            infoRow.Add(distanceStack);

            // Info casiers si disponibles
            if (borne.HasCasiers)
            {
                var casierStack = new HorizontalStackLayout
                {
                    Spacing = 6
                };

                var lockerImage = new Image
                {
                    Source = "locker.png",
                    HeightRequest = 12,
                    WidthRequest = 12,
                    VerticalOptions = LayoutOptions.Center
                };
                casierStack.Add(lockerImage);

                var casierLabel = new Label
                {
                    Text = borne.CasiersInfo,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#FFD602"),
                    FontAttributes = FontAttributes.Bold
                };
                casierStack.Add(casierLabel);
                infoRow.Add(casierStack);
            }

            infoStack.Add(infoRow);
            contentGrid.Add(infoStack, 1, 0);

            // Statut et barre de progression (colonne de droite)
            var statusStack = new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.End
            };

            string statusColor;
            string statusText;

            if (borne.IsInMaintenance)
            {
                statusColor = "#F44336";
                statusText = "Maintenance";
            }
            else if (borne.IsAvailable)
            {
                statusColor = "#FFD602";
                statusText = "Disponible";
            }
            else
            {
                statusColor = "#BBBBBB";
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

            var progressBarContainer = new Grid
            {
                HeightRequest = 6,
                WidthRequest = 100,
                VerticalOptions = LayoutOptions.Center
            };

            var progressBackground = new Rectangle
            {
                Fill = Color.FromArgb("#333333"),
                WidthRequest = 100,
                HeightRequest = 6
            };

            var progressFill = new Rectangle
            {
                Fill = Color.FromArgb(statusColor),
                WidthRequest = 100 * borne.ChargePercentage / 100,
                HeightRequest = 6,
                HorizontalOptions = LayoutOptions.Start
            };

            progressBarContainer.Add(progressBackground);
            progressBarContainer.Add(progressFill);
            progressStack.Add(progressBarContainer, 0, 0);

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

            return contentGrid;
        }

        private VerticalStackLayout CreateCasierContainer(BorneModel borne)
        {
            var casierContainer = new VerticalStackLayout
            {
                Spacing = 0,
                IsVisible = false, // Initialement masqué
                BackgroundColor = Color.FromArgb("#1A1F2E"),
                Padding = new Thickness(15, 10)
            };

            // En-tête des casiers avec icône de flèche
            var headerStack = new HorizontalStackLayout
            {
                Spacing = 8,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var lockerIcon = new Image
            {
                Source = "locker.png",
                HeightRequest = 16,
                WidthRequest = 16,
                VerticalOptions = LayoutOptions.Center
            };
            headerStack.Add(lockerIcon);

            var headerLabel = new Label
            {
                Text = $"Casiers disponibles ({borne.CasiersCount})",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };
            headerStack.Add(headerLabel);

            // Icône de flèche pour indiquer l'état du menu
            var arrowIcon = new Label
            {
                Text = "▼",
                FontSize = 12,
                TextColor = Color.FromArgb("#FFD602"),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };
            headerStack.Add(arrowIcon);

            casierContainer.Add(headerStack);

            // Liste des casiers
            for (int i = 0; i < borne.Casiers.Count; i++)
            {
                var casier = borne.Casiers[i];
                var casierCard = CreateCasierCard(casier, i + 1);
                casierContainer.Add(casierCard);
            }

            // Si aucun casier, afficher un message
            if (borne.Casiers.Count == 0)
            {
                var noLockersLabel = new Label
                {
                    Text = "Aucun casier disponible pour cette borne",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#BBBBBB"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 10)
                };
                casierContainer.Add(noLockersLabel);
            }

            return casierContainer;
        }

        // 8. Ajouter une méthode pour ajuster la taille des casiers
        private Border CreateCasierCard(Casier casier, int casierNumber)
        {
            var casierBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#2A2F3E"),
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                Margin = new Thickness(0, 2),
                Padding = new Thickness(12, 10)
            };

            var casierGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = new GridLength(110) } // Largeur fixe pour la colonne du bouton
                }
            };

            // Icône du casier
            var lockerImage = new Image
            {
                Source = "locker.png",
                HeightRequest = 20,
                WidthRequest = 20,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 0, 12, 0)
            };

            casierGrid.Add(lockerImage, 0, 0);

            // Informations du casier
            var casierInfo = new VerticalStackLayout
            {
                Spacing = 3,
                VerticalOptions = LayoutOptions.Center
            };

            var casierNameLabel = new Label
            {
                Text = $"Casier {casierNumber}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };
            casierInfo.Add(casierNameLabel);

            var statusLabel = new Label
            {
                Text = casier.StatusText,
                FontSize = 12,
                TextColor = casier.StatusColor,
                FontAttributes = FontAttributes.Bold
            };
            casierInfo.Add(statusLabel);

            casierGrid.Add(casierInfo, 1, 0);

            // Bouton d'action (réserver/annuler/indisponible)
            Button actionButton = CreateCasierActionButton(casier);

            casierGrid.Add(actionButton, 2, 0);
            casierBorder.Content = casierGrid;
            return casierBorder;
        }

        // 1. Modifier la méthode CreateCasierActionButton pour augmenter la largeur du bouton
        private Button CreateCasierActionButton(Casier casier)
        {
            // Vérifier si c'est l'utilisateur actuel qui a réservé ce casier
            int? currentUserId = SessionService.CurrentUserId;
            bool isMyReservation = currentUserId.HasValue && casier.UserId == currentUserId.Value && casier.IsReserved;

            if (isMyReservation)
            {
                // Casier réservé par l'utilisateur actuel - bouton "Annuler"
                var button = new Button
                {
                    Text = "Annuler",
                    FontSize = 9, // Réduit de 10 à 9
                    BackgroundColor = Color.FromArgb("#F44336"), // Rouge
                    TextColor = Colors.White,
                    CornerRadius = 12, // Réduit de 15 à 12
                    HeightRequest = 28, // Réduit de 32 à 28
                    WidthRequest = 85, // Réduit de 105 à 85
                    Padding = new Thickness(2, 1), // Réduit le padding
                    IsEnabled = true,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold
                };
                button.Clicked += async (s, e) => await OnAnnulerReservationClicked(casier);
                return button;
            }
            else if (casier.IsAvailable)
            {
                // Casier libre - vérifier si l'utilisateur a déjà une réservation active
                bool hasActiveReservation = CheckUserHasActiveReservation();

                var button = new Button
                {
                    Text = "Réserver",
                    FontSize = 9, // Réduit encore de 10 à 9
                    BackgroundColor = hasActiveReservation ? Color.FromArgb("#999999") : Color.FromArgb("#FFD602"),
                    TextColor = hasActiveReservation ? Colors.White : Colors.Black,
                    CornerRadius = 12, // Réduit de 15 à 12
                    HeightRequest = 28, // Réduit de 32 à 28
                    WidthRequest = 85, // Réduit de 105 à 85
                    Padding = new Thickness(2, 1), // Réduit le padding
                    IsEnabled = !hasActiveReservation,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold // Ajout pour améliorer la lisibilité
                };

                if (hasActiveReservation)
                {
                    // Ajouter un gestionnaire pour expliquer pourquoi le bouton est désactivé
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += async (s, e) => {
                        await DisplayAlert("Réservation impossible", "Vous avez déjà une réservation active. Vous ne pouvez réserver qu'un seul casier à la fois.", "OK");
                    };
                    button.GestureRecognizers.Add(tapGesture);
                }
                else
                {
                    button.Clicked += async (s, e) => await OnReserveCasierClicked(casier);
                }

                return button;
            }
            else
            {
                // Casier indisponible (réservé par quelqu'un d'autre ou occupé)
                return new Button
                {
                    Text = "Indisponible",
                    FontSize = 8, // Réduit de 9 à 8 car le texte est plus long
                    BackgroundColor = Color.FromArgb("#666666"),
                    TextColor = Colors.White,
                    CornerRadius = 12, // Réduit de 15 à 12
                    HeightRequest = 28, // Réduit de 32 à 28
                    WidthRequest = 85, // Réduit de 105 à 85
                    Padding = new Thickness(1, 1), // Padding minimal
                    IsEnabled = false,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold
                };
            }
        }

        private bool CheckUserHasActiveReservation()
        {
            if (!SessionService.IsLoggedIn) return false;

            int? currentUserId = SessionService.CurrentUserId;
            if (!currentUserId.HasValue) return false;

            // Vérifier dans toutes les bornes si l'utilisateur a une réservation active
            if (_viewModel?.Bornes != null)
            {
                foreach (var borne in _viewModel.Bornes)
                {
                    foreach (var casier in borne.Casiers)
                    {
                        if (casier.UserId == currentUserId.Value && (casier.IsReserved || casier.IsOccupied))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // Modification de la méthode OnReserveCasierClicked pour rediriger vers CodePage si pas de code
        private async Task OnReserveCasierClicked(Casier casier)
        {
            try
            {
                if (_currentUser == null || !SessionService.IsLoggedIn)
                {
                    await DisplayAlert("Erreur", "Aucun utilisateur connecté. Veuillez vous connecter.", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                    return;
                }

                int? userIdNullable = SessionService.CurrentUserId;
                if (!userIdNullable.HasValue)
                {
                    await DisplayAlert("Erreur", "Impossible de récupérer l'ID utilisateur.", "OK");
                    return;
                }
                int userId = userIdNullable.Value;

                // Vérifier si l'utilisateur a déjà une réservation active
                var reservationActive = await _casierService.GetReservationActive(userId);
                if (reservationActive != null)
                {
                    await DisplayAlert(
                        "Réservation impossible",
                        $"Vous avez déjà une réservation active pour le casier {reservationActive.CasierId}. Vous ne pouvez réserver qu'un seul casier à la fois.",
                        "OK");
                    return;
                }

                // Vérifier si l'utilisateur a un code de casier configuré
                if (string.IsNullOrEmpty(_currentUser.CodeCasiers))
                {
                    bool goToCodePage = await DisplayAlert(
                        "Code manquant",
                        "Vous devez configurer un code personnel avant de pouvoir réserver un casier. Voulez-vous le faire maintenant ?",
                        "Configurer",
                        "Annuler");

                    if (goToCodePage)
                    {
                        await Navigation.PushAsync(new CodePage());
                    }
                    return;
                }

                // Afficher une boîte de dialogue pour confirmer la réservation
                bool confirm = await DisplayAlert(
                    "Réservation",
                    $"Voulez-vous réserver le casier {casier.CasierId} ?\n\nVotre code personnel sera utilisé pour déverrouiller ce casier.",
                    "Réserver",
                    "Annuler");

                if (confirm)
                {
                    // Demander à l'utilisateur de confirmer son code
                    string? enteredCode = await DisplayPromptAsync(
                        "Confirmation du code",
                        "Veuillez entrer votre code personnel pour confirmer la réservation :",
                        "Confirmer",
                        "Annuler",
                        placeholder: "Entrez votre code",
                        maxLength: 10,
                        keyboard: Keyboard.Numeric,
                        initialValue: "");

                    if (string.IsNullOrEmpty(enteredCode))
                    {
                        return;
                    }

                    if (enteredCode != _currentUser.CodeCasiers)
                    {
                        await DisplayAlert("Code incorrect", "Le code entré ne correspond pas à votre code personnel.", "OK");
                        return;
                    }

                    // Le code est correct, procéder à la réservation
                    bool success = await _casierService.ReserverCasierAsync(casier.CasierId, userId);

                    if (success)
                    {
                        await DisplayAlert("Succès", $"Casier réservé avec succès ! Utilisez votre code personnel ({_currentUser.CodeCasiers}) pour déverrouiller le casier.", "OK");

                        // Rafraîchir l'affichage
                        await RefreshStationCasiers(casier.BorneId);
                        await RefreshAllStations();
                    }
                    else
                    {
                        await DisplayAlert("Erreur", "Impossible de réserver ce casier.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Une erreur s'est produite : {ex.Message}", "OK");
            }
        }

        // Modification de la méthode OnAnnulerReservationClicked pour utiliser SessionService
        private async Task OnAnnulerReservationClicked(Casier casier)
        {
            try
            {
                if (_currentUser == null || !SessionService.IsLoggedIn)
                {
                    await DisplayAlert("Erreur", "Aucun utilisateur connecté.", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                    return;
                }

                int? userIdNullable = SessionService.CurrentUserId;
                if (!userIdNullable.HasValue)
                {
                    await DisplayAlert("Erreur", "Impossible de récupérer l'ID utilisateur.", "OK");
                    return;
                }
                int userId = userIdNullable.Value;

                // Confirmer l'annulation
                bool confirm = await DisplayAlert(
                    "Annuler la réservation",
                    $"Voulez-vous vraiment annuler votre réservation du casier {casier.CasierId} ?",
                    "Annuler la réservation",
                    "Garder la réservation");

                if (confirm)
                {
                    bool success = await _casierService.AnnulerReservationAsync(casier.CasierId, userId);

                    if (success)
                    {
                        await DisplayAlert("Succès", "Votre réservation a été annulée avec succès.", "OK");

                        // Rafraîchir l'affichage
                        await RefreshStationCasiers(casier.BorneId);
                        await RefreshAllStations();
                    }
                    else
                    {
                        await DisplayAlert("Erreur", "Impossible d'annuler cette réservation.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Une erreur s'est produite : {ex.Message}", "OK");
            }
        }

        private async Task RefreshStationCasiers(int borneId)
        {
            try
            {
                // Trouver la borne dans le ViewModel
                var borne = _viewModel?.Bornes.FirstOrDefault(b => b.BorneId == borneId);
                if (borne != null)
                {
                    // Recharger les casiers
                    var casiers = await _casierService.GetCasiersByBorneIdAsync(borneId);
                    borne.Casiers = casiers ?? new List<Casier>();

                    // Recréer la carte de la station
                    if (_stationCards.ContainsKey(borne.Id) && _stationListLayout != null)
                    {
                        var oldCard = _stationCards[borne.Id];
                        var index = _stationListLayout.Children.IndexOf(oldCard);

                        if (index >= 0)
                        {
                            _stationListLayout.Children.RemoveAt(index);
                            var newCard = CreateStationCard(borne);
                            _stationListLayout.Children.Insert(index, newCard);
                            _stationCards[borne.Id] = newCard;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du rafraîchissement des casiers: {ex.Message}");
            }
        }

        // Nouvelle méthode pour rafraîchir toutes les bornes
        private async Task RefreshAllStations()
        {
            if (_viewModel == null || _stationListLayout == null) return;

            try
            {
                // Recharger les casiers pour chaque borne
                foreach (var borne in _viewModel.Bornes)
                {
                    var casiers = await _casierService.GetCasiersByBorneIdAsync(borne.BorneId);
                    borne.Casiers = casiers ?? new List<Casier>();
                }

                // Recréer toutes les cartes de stations
                _stationListLayout.Clear();
                _stationCards.Clear();
                _casierContainers.Clear();

                var sortedBornes = _viewModel.Bornes.OrderByDescending(b => b.IsAvailable).ToList();

                foreach (var borne in sortedBornes)
                {
                    var stationCard = CreateStationCard(borne);
                    _stationListLayout.Add(stationCard);
                    _stationCards[borne.Id] = stationCard;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du rafraîchissement des stations: {ex.Message}");
            }
        }

        private void ToggleStationSelection(int stationId)
        {
            // Si c'est la même station, basculer l'affichage des casiers
            if (_selectedStationId == stationId)
            {
                ToggleCasierVisibility(stationId);
            }
            else
            {
                // Désélectionner l'ancienne station
                if (_selectedStationId != -1)
                {
                    DeselectStation(_selectedStationId);
                }

                // Sélectionner la nouvelle station
                SelectStation(stationId);
                ZoomToStation(stationId);
            }
        }

        private void ToggleCasierVisibility(int stationId)
        {
            var borne = _viewModel?.Bornes.FirstOrDefault(b => b.Id == stationId);
            if (borne != null && _casierContainers.ContainsKey(borne.BorneId))
            {
                var container = _casierContainers[borne.BorneId];
                container.IsVisible = !container.IsVisible;
            }
        }

        private void SelectStation(int stationId)
        {
            _selectedStationId = stationId;
            if (_stationCards.ContainsKey(stationId))
            {
                _stationCards[stationId].StrokeThickness = 2;
                _stationCards[stationId].Stroke = Color.FromArgb("#FFD602");

                _mainScrollView?.ScrollToAsync(0, 0, true);

                if (_mapContainer != null)
                {
                    double maxHeight = _isMobileDevice ? 400 : 490;
                    _mapContainer.HeightRequest = maxHeight;
                }

                // Afficher les casiers si disponibles
                var borne = _viewModel?.Bornes.FirstOrDefault(b => b.Id == stationId);
                if (borne != null && _casierContainers.ContainsKey(borne.BorneId))
                {
                    _casierContainers[borne.BorneId].IsVisible = true;
                }
            }
        }

        private void DeselectStation(int stationId)
        {
            if (_stationCards.ContainsKey(stationId))
            {
                _stationCards[stationId].StrokeThickness = 0;
                _stationCards[stationId].Stroke = null;

                // Masquer les casiers
                var borne = _viewModel?.Bornes.FirstOrDefault(b => b.Id == stationId);
                if (borne != null && _casierContainers.ContainsKey(borne.BorneId))
                {
                    _casierContainers[borne.BorneId].IsVisible = false;
                }
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null || string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                if (_viewModel != null)
                {
                    var sortedBornes = _viewModel.Bornes.OrderByDescending(b => b.IsAvailable).ToList();
                    UpdateStationList(sortedBornes);
                }
                return;
            }

            var searchText = e.NewTextValue.ToLowerInvariant();
            var filteredBornes = _viewModel.Bornes.Where(b =>
                b.Name.ToLowerInvariant().Contains(searchText) ||
                b.Address.ToLowerInvariant().Contains(searchText)).ToList();

            UpdateStationList(filteredBornes);
        }

        private void UpdateStationList(IEnumerable<BorneModel> bornes)
        {
            if (_stationListLayout == null) return;

            _stationListLayout.Clear();
            _stationCards.Clear();
            _casierContainers.Clear();

            var sortedBornes = bornes.OrderByDescending(b => b.IsAvailable).ToList();

            foreach (var borne in sortedBornes)
            {
                var stationCard = CreateStationCard(borne);
                _stationListLayout.Add(stationCard);
                _stationCards[borne.Id] = stationCard;
            }

            _selectedStationId = -1;
        }

        private async Task NavigateToStation(int stationId, string address, double latitude, double longitude)
        {
            try
            {
                var borne = _viewModel?.Bornes.FirstOrDefault(b => b.Id == stationId);
                if (borne == null)
                {
                    Debug.WriteLine($"Borne non trouvée avec l'ID: {stationId}");
                    return;
                }

                string formattedAddress = $"{borne.Address}, {borne.PostalCode} {borne.City}";
                Debug.WriteLine($"[DEBUG] Navigation vers: {formattedAddress} ({latitude}, {longitude})");

                bool useGoogleMaps = await DisplayAlert(
                    "Navigation",
                    $"Naviguer vers {borne.Name}?",
                    "Google Maps",
                    "Maps par défaut");

                string navigationUri;

                if (useGoogleMaps)
                {
                    navigationUri = $"https://www.google.com/maps/dir/?api=1&destination={Uri.EscapeDataString(formattedAddress)}&travelmode=driving";
                }
                else
                {
                    navigationUri = $"geo:0,0?q={Uri.EscapeDataString(formattedAddress)}";

                    if (DeviceInfo.Platform == DevicePlatform.iOS)
                    {
                        navigationUri = $"maps://?daddr={Uri.EscapeDataString(formattedAddress)}&dirflg=d";
                    }
                }

                try
                {
                    await Launcher.OpenAsync(navigationUri);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erreur lors de l'ouverture de l'application de navigation: {ex.Message}");

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

        private void SetupNavigation()
        {
            var accueilTab = this.FindByName<VerticalStackLayout>("AccueilTab");
            if (accueilTab != null)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => {
                    await Navigation.PopAsync();
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

        private async void OnMyLocationClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized)
                {
                    await DisplayAlert("Erreur", "La carte n'est pas encore initialisée. Veuillez réessayer dans quelques instants.", "OK");
                    return;
                }

                var mapWebView = GetMapWebView();
                if (mapWebView != null)
                {
                    await mapWebView.EvaluateJavaScriptAsync("centerOnUserLocation()");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors de la géolocalisation: {ex.Message}", "OK");
            }
        }

        private async void OnLayersClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized)
                {
                    await DisplayAlert("Erreur", "La carte n'est pas encore initialisée. Veuillez réessayer dans quelques instants.", "OK");
                    return;
                }

                var mapWebView = GetMapWebView();
                if (mapWebView != null)
                {
                    await mapWebView.EvaluateJavaScriptAsync("toggle3DView()");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du changement de vue: {ex.Message}", "OK");
            }
        }

        private async void OnCenterClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized)
                {
                    await DisplayAlert("Erreur", "La carte n'est pas encore initialisée. Veuillez réessayer dans quelques instants.", "OK");
                    return;
                }

                var mapWebView = GetMapWebView();
                if (mapWebView != null)
                {
                    await mapWebView.EvaluateJavaScriptAsync("centerMap()");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du recentrage: {ex.Message}", "OK");
            }
        }

        private async void OnZoomInClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized) return;
                var mapWebView = GetMapWebView();
                if (mapWebView != null)
                {
                    await mapWebView.EvaluateJavaScriptAsync("zoomIn()");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du zoom: {ex.Message}", "OK");
            }
        }

        private async void OnZoomOutClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!_mapInitialized) return;
                var mapWebView = GetMapWebView();
                if (mapWebView != null)
                {
                    await mapWebView.EvaluateJavaScriptAsync("zoomOut()");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du zoom: {ex.Message}", "OK");
            }
        }

        private async void ZoomToStation(int stationId)
        {
            try
            {
                if (!_mapInitialized) return;

                var mapWebView = GetMapWebView();
                if (mapWebView != null)
                {
                    await mapWebView.EvaluateJavaScriptAsync($"zoomToStation({stationId})");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur lors du zoom vers la station: {ex.Message}", "OK");
            }
        }

        // Méthodes pour les mises à jour en temps réel
        private void StartRealTimeUpdates()
        {
            try
            {
                // Arrêter le timer existant s'il y en a un
                StopRealTimeUpdates();

                // Intervalle plus long sur mobile pour économiser la batterie
                int updateInterval = _isMobileDevice ? 60000 : 30000; // 60s mobile, 30s desktop

                // Créer un nouveau timer pour les mises à jour
                _casierUpdateTimer = new System.Timers.Timer(updateInterval);
                _casierUpdateTimer.Elapsed += OnCasierUpdateTimer;
                _casierUpdateTimer.AutoReset = true;
                _casierUpdateTimer.Enabled = true;

                Debug.WriteLine($"[MapPage] Mises à jour en temps réel démarrées (intervalle: {updateInterval}ms)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors du démarrage des mises à jour: {ex.Message}");
            }
        }

        private void StopRealTimeUpdates()
        {
            try
            {
                if (_casierUpdateTimer != null)
                {
                    _casierUpdateTimer.Stop();
                    _casierUpdateTimer.Dispose();
                    _casierUpdateTimer = null;
                    Debug.WriteLine("[MapPage] Mises à jour en temps réel arrêtées");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors de l'arrêt des mises à jour: {ex.Message}");
            }
        }

        private async void OnCasierUpdateTimer(object? sender, ElapsedEventArgs e)
        {
            if (_isUpdatingCasiers) return;

            try
            {
                _isUpdatingCasiers = true;
                await MainThread.InvokeOnMainThreadAsync(async () => {
                    await RefreshAllStations();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapPage] Erreur lors de la mise à jour automatique: {ex.Message}");
            }
            finally
            {
                _isUpdatingCasiers = false;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopRealTimeUpdates();
        }

        // Classe pour l'interface JavaScript sur Android
#if ANDROID
        public class WebViewJavaScriptInterface : Java.Lang.Object
        {
            private readonly MapPage _mapPage;

            public WebViewJavaScriptInterface(MapPage mapPage)
            {
                _mapPage = mapPage;
            }

            [Android.Webkit.JavascriptInterface]
            public void PostMessage(string message)
            {
                MainThread.InvokeOnMainThreadAsync(() => {
                    _ = _mapPage.ProcessBridgeMessage(message);
                });
            }
        }
#endif
    }
}
