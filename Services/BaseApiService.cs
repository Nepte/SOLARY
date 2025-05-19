using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

namespace SOLARY.Services
{
    public abstract class BaseApiService
    {
        protected readonly HttpClient _httpClient;
        protected readonly JsonSerializerOptions _jsonOptions;

        public string BaseUrl { get; }

        protected BaseApiService(string baseUrl)
        {
            BaseUrl = baseUrl;

            var handler = new HttpClientHandler
            {
                // Pour ignorer les certificats en dev
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };

            // Configuration du JsonSerializer pour permettre les conversions nécessaires
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                Converters = {
                    new BooleanConverter(),
                    new DateTimeConverter(),
                    new NullableDateTimeConverter()
                }
            };
        }

        // Convertisseur personnalisé pour gérer les booléens représentés par des entiers
        private class BooleanConverter : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    return reader.GetInt32() != 0;
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    string stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                        return false;

                    if (int.TryParse(stringValue, out int intValue))
                        return intValue != 0;

                    if (bool.TryParse(stringValue, out bool boolValue))
                        return boolValue;

                    return false;
                }
                return reader.GetBoolean();
            }

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
                writer.WriteBooleanValue(value);
            }
        }

        // Convertisseur personnalisé pour gérer les dates
        private class DateTimeConverter : JsonConverter<DateTime>
        {
            private static readonly string[] DateFormats = new[]
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy/MM/dd",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy"
            };

            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    string dateString = reader.GetString();
                    if (DateTime.TryParseExact(dateString, DateFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime date))
                    {
                        return date;
                    }

                    if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out date))
                    {
                        return date;
                    }

                    throw new JsonException($"Unable to parse date: {dateString}");
                }

                throw new JsonException("Expected string value for DateTime");
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        // Convertisseur personnalisé pour gérer les dates nullables
        private class NullableDateTimeConverter : JsonConverter<DateTime?>
        {
            private static readonly string[] DateFormats = new[]
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy/MM/dd",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy"
            };

            public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    string dateString = reader.GetString();
                    if (string.IsNullOrEmpty(dateString))
                    {
                        return null;
                    }

                    if (DateTime.TryParseExact(dateString, DateFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime date))
                    {
                        return date;
                    }

                    if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out date))
                    {
                        return date;
                    }

                    throw new JsonException($"Unable to parse date: {dateString}");
                }

                throw new JsonException("Expected string value for DateTime");
            }

            public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                {
                    writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
        }

        // Méthode générique pour faire un GET
        protected async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                Debug.WriteLine($"[DEBUG] GET -> {endpoint}");

                var response = await _httpClient.GetAsync(endpoint);
                string responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[DEBUG] Réponse brute : {responseContent}");

                // Vérifications
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    Debug.WriteLine("[ERREUR] Réponse vide");
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[ERREUR] Connexion API : {ex.Message}");
                return default;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Exception lors du GET : {ex.Message}");
                return default;
            }
        }

        // Méthode générique pour faire un POST
        protected async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                // 1) Sérialisation en JSON
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"[DEBUG] POST -> {endpoint}, data = {json}");

                // 2) Appel HTTP POST
                var response = await _httpClient.PostAsync(endpoint, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[DEBUG] Réponse brute : {responseContent}");

                // 3) Vérifications
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    Debug.WriteLine("[ERREUR] Réponse vide");
                    return default;
                }

                // 4) Désérialisation
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[ERREUR] Connexion API : {ex.Message}");
                return default;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Exception lors du POST : {ex.Message}");
                return default;
            }
        }

        // Méthode générique pour faire un PUT
        protected async Task<T?> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"[DEBUG] PUT -> {endpoint}, data = {json}");

                var response = await _httpClient.PutAsync(endpoint, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[DEBUG] Réponse brute : {responseContent}");

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    Debug.WriteLine("[ERREUR] Réponse vide");
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Exception lors du PUT : {ex.Message}");
                return default;
            }
        }

        // Méthode générique pour faire un DELETE
        protected async Task<T?> DeleteAsync<T>(string endpoint)
        {
            try
            {
                Debug.WriteLine($"[DEBUG] DELETE -> {endpoint}");

                var response = await _httpClient.DeleteAsync(endpoint);
                string responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[DEBUG] Réponse brute : {responseContent}");

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    Debug.WriteLine("[ERREUR] Réponse vide");
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERREUR] Exception lors du DELETE : {ex.Message}");
                return default;
            }
        }
    }
}
