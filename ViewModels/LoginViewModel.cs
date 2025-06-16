using SOLARY.Services;
using SOLARY.Views;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Text.Json;

namespace SOLARY.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly UserService _userService;
        private bool _isNavigating = false;

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // ✨ CORRIGÉ: Propriété pour "Se souvenir de moi" avec binding bidirectionnel
        private bool _rememberMe = false;
        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                if (_rememberMe != value)
                {
                    _rememberMe = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CheckboxBackgroundColor));
                    OnPropertyChanged(nameof(CheckboxBorderColor));
                    OnPropertyChanged(nameof(CheckmarkVisibility));
                    Debug.WriteLine($"[DEBUG] RememberMe changé: {value}");
                }
            }
        }

        // Propriétés pour gérer l'apparence de la checkbox
        public Color CheckboxBackgroundColor => RememberMe ? Color.FromArgb("#FFD602") : Colors.Transparent;
        public Color CheckboxBorderColor => Colors.White;
        public bool CheckmarkVisibility => RememberMe;

        public ICommand LoginCommand { get; }
        public ICommand NavigateToRegisterCommand { get; }
        public ICommand ToggleRememberMeCommand { get; }

        public LoginViewModel()
        {
            _authService = new AuthService();
            _userService = new UserService();
            LoginCommand = new Command(async () => await Login());
            NavigateToRegisterCommand = new Command(async () => await NavigateToRegister());
            // ✨ CORRIGÉ: Commande toggle avec debug
            ToggleRememberMeCommand = new Command(() =>
            {
                Debug.WriteLine($"[DEBUG] Toggle RememberMe: {RememberMe} -> {!RememberMe}");
                RememberMe = !RememberMe;
            });
        }

        // Méthode publique pour initialiser la vérification auto-login
        public async Task InitializeAsync()
        {
            await CheckAutoLogin();
        }

        private async Task Login()
        {
            if (_isNavigating) return;

            var page = GetCurrentPage();
            if (page == null) return;

            if (!await ValidateInputs()) return;

            try
            {
                Debug.WriteLine($"[DEBUG] Tentative de connexion pour {Email}");
                Debug.WriteLine($"[DEBUG] RememberMe état: {RememberMe}");

                var response = await _authService.Login(Email, Password);

                if (response == null)
                {
                    await ShowAlertAsync("Erreur Connexion", "Aucune réponse de l'API.");
                    return;
                }

                Debug.WriteLine($"[DEBUG] Réponse API Login: Success={response.Success}, Message={response.Message}");

                if (response.Success)
                {
                    // Extraire l'ID utilisateur de la réponse
                    int userId = await ExtractUserIdFromResponse(response);

                    if (userId > 0)
                    {
                        // Sauvegarder la session normale
                        SessionService.SaveUserSession(userId, Email);

                        // ✨ CORRIGÉ: Si "Se souvenir de moi" est coché, sauvegarder pour 2 semaines
                        if (RememberMe)
                        {
                            var expirationDate = DateTime.Now.AddDays(14); // 2 semaines
                            var sessionData = $"{userId}|{Email}|{expirationDate:yyyy-MM-dd HH:mm:ss}";
                            await SecureStorage.SetAsync("RememberMeSession", sessionData);
                            Debug.WriteLine($"[DEBUG] ✅ Session 'Se souvenir de moi' sauvegardée jusqu'au {expirationDate}");
                            Debug.WriteLine($"[DEBUG] ✅ Données sauvegardées: {sessionData}");
                        }
                        else
                        {
                            Debug.WriteLine("[DEBUG] ❌ RememberMe non coché, pas de sauvegarde longue durée");
                        }

                        await ShowAlertAsync("Succès", "Connexion réussie !");
                        await NavigateToHomePage();
                    }
                    else
                    {
                        await ShowAlertAsync("Erreur", "Impossible de récupérer les informations utilisateur.");
                    }
                }
                else
                {
                    await ShowAlertAsync("Erreur", response.Message ?? "Identifiants incorrects ou compte non vérifié.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Exception lors de la connexion : {ex.Message}");
                await ShowAlertAsync("Erreur Exception", $"Une erreur s'est produite : {ex.Message}");
            }
        }

        // Méthode pour vérifier la connexion automatique (appelée depuis LoginPage seulement)
        private async Task CheckAutoLogin()
        {
            try
            {
                Debug.WriteLine("[DEBUG] LoginPage: Vérification de la connexion automatique...");

                var savedSession = await SecureStorage.GetAsync("RememberMeSession");
                if (!string.IsNullOrEmpty(savedSession))
                {
                    Debug.WriteLine("[DEBUG] LoginPage: Session sauvegardée trouvée");

                    var sessionParts = savedSession.Split('|');
                    if (sessionParts.Length >= 3)
                    {
                        var userId = sessionParts[0];
                        var email = sessionParts[1];
                        var expirationDate = DateTime.Parse(sessionParts[2]);

                        if (DateTime.Now < expirationDate)
                        {
                            Debug.WriteLine($"[DEBUG] LoginPage: Session valide pour {email}, connexion automatique...");

                            SessionService.SaveUserSession(int.Parse(userId), email);
                            await SafeNavigateToHomePage();
                            return;
                        }
                        else
                        {
                            Debug.WriteLine("[DEBUG] LoginPage: Session expirée, suppression...");
                            SecureStorage.Remove("RememberMeSession");
                        }
                    }
                }

                Debug.WriteLine("[DEBUG] LoginPage: Aucune session automatique trouvée");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG] LoginPage: Erreur lors de la vérification: {ex.Message}");
            }
        }

        private async Task SafeNavigateToHomePage()
        {
            if (_isNavigating) return;

            try
            {
                _isNavigating = true;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await NavigateToHomePage();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG] Erreur lors de la navigation sécurisée: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async Task<int> ExtractUserIdFromResponse(ApiResponse response)
        {
            try
            {
                Debug.WriteLine($"[DEBUG] Extraction de l'ID utilisateur de la réponse API");

                // Si l'API retourne directement l'ID utilisateur dans Data
                if (response.Data != null)
                {
                    Debug.WriteLine($"[DEBUG] Data reçu: {response.Data}");

                    // Cas 1: Data contient directement l'ID
                    if (response.Data is JsonElement jsonElement)
                    {
                        Debug.WriteLine($"[DEBUG] Data est un JsonElement");

                        // Essayer de récupérer user_id
                        if (jsonElement.TryGetProperty("user_id", out var userIdProp))
                        {
                            if (userIdProp.TryGetInt32(out int userId))
                            {
                                Debug.WriteLine($"[DEBUG] ID utilisateur trouvé dans user_id: {userId}");
                                return userId;
                            }
                        }

                        // Essayer de récupérer id
                        if (jsonElement.TryGetProperty("id", out var idProp))
                        {
                            if (idProp.TryGetInt32(out int userId2))
                            {
                                Debug.WriteLine($"[DEBUG] ID utilisateur trouvé dans id: {userId2}");
                                return userId2;
                            }
                        }

                        // Cas 2: Data contient un objet user
                        if (jsonElement.TryGetProperty("user", out var userProp))
                        {
                            Debug.WriteLine($"[DEBUG] Objet user trouvé");
                            if (userProp.TryGetProperty("user_id", out var userIdProp2))
                            {
                                if (userIdProp2.TryGetInt32(out int userId3))
                                {
                                    Debug.WriteLine($"[DEBUG] ID utilisateur trouvé dans user.user_id: {userId3}");
                                    return userId3;
                                }
                            }

                            if (userProp.TryGetProperty("id", out var idProp2))
                            {
                                if (idProp2.TryGetInt32(out int userId4))
                                {
                                    Debug.WriteLine($"[DEBUG] ID utilisateur trouvé dans user.id: {userId4}");
                                    return userId4;
                                }
                            }
                        }
                    }

                    // Cas 3: Conversion directe si c'est un entier
                    if (int.TryParse(response.Data.ToString(), out int directId))
                    {
                        Debug.WriteLine($"[DEBUG] ID utilisateur converti directement: {directId}");
                        return directId;
                    }

                    // Cas 4: Si Data est un string JSON, essayer de le parser
                    if (response.Data is string jsonString)
                    {
                        Debug.WriteLine($"[DEBUG] Data est un string JSON: {jsonString}");
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(jsonString);
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("user_id", out var userIdProp))
                            {
                                if (userIdProp.TryGetInt32(out int userId))
                                {
                                    Debug.WriteLine($"[DEBUG] ID utilisateur trouvé dans JSON string: {userId}");
                                    return userId;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[DEBUG] Erreur lors du parsing JSON: {ex.Message}");
                        }
                    }
                }

                // Fallback: Rechercher l'utilisateur par email
                Debug.WriteLine($"[DEBUG] ID non trouvé dans la réponse, recherche par email: {Email}");
                return await FindUserIdByEmail(Email);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG] Erreur lors de l'extraction de l'ID utilisateur: {ex.Message}");
                return await FindUserIdByEmail(Email);
            }
        }

        private async Task<int> FindUserIdByEmail(string email)
        {
            try
            {
                Debug.WriteLine($"[DEBUG] Recherche de l'utilisateur par email: {email}");

                // Essayer d'abord avec l'endpoint GetUserByEmail si il existe
                try
                {
                    var user = await _userService.GetUserByEmail(email);
                    if (user != null)
                    {
                        Debug.WriteLine($"[DEBUG] Utilisateur trouvé par GetUserByEmail: ID {user.UserId}");
                        return user.UserId;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DEBUG] GetUserByEmail non disponible: {ex.Message}");
                }

                // Fallback: Essayer quelques IDs courants
                var knownUserIds = new[] { 1, 2, 3, 4, 5, 24, 25, 26, 27, 28, 29, 30 };

                foreach (int testId in knownUserIds)
                {
                    try
                    {
                        var user = await _userService.GetUserById(testId);
                        if (user != null && user.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"[DEBUG] Utilisateur trouvé par recherche: ID {testId} pour {email}");
                            return testId;
                        }
                    }
                    catch
                    {
                        // Ignorer les erreurs pour les IDs qui n'existent pas
                    }
                }

                Debug.WriteLine($"[DEBUG] Aucun utilisateur trouvé pour l'email: {email}");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG] Erreur lors de la recherche par email: {ex.Message}");
                return 0;
            }
        }

        private async Task<bool> ValidateInputs()
        {
            if (string.IsNullOrEmpty(Email) || !Email.Contains("@") || !Email.Contains("."))
            {
                await ShowAlertAsync("Erreur", "Adresse e-mail invalide.");
                return false;
            }

            if (string.IsNullOrEmpty(Password))
            {
                await ShowAlertAsync("Erreur", "Veuillez entrer un mot de passe.");
                return false;
            }

            return true;
        }

        private async Task NavigateToRegister()
        {
            if (_isNavigating) return;

            var page = GetCurrentPage();
            if (page?.Navigation != null)
            {
                _isNavigating = true;
                try
                {
                    await page.Navigation.PushAsync(new RegisterPage());
                }
                finally
                {
                    _isNavigating = false;
                }
            }
        }

        private async Task NavigateToHomePage()
        {
            if (_isNavigating) return;

            var page = GetCurrentPage();
            if (page?.Navigation != null)
            {
                _isNavigating = true;
                try
                {
                    await page.Navigation.PushAsync(new HomePage());
                }
                finally
                {
                    _isNavigating = false;
                }
            }
        }

        private async Task ShowAlertAsync(string title, string message)
        {
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlert(title, message, "OK");
            }
        }

        private Page? GetCurrentPage()
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page;
        }
    }
}
