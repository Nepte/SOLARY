using SOLARY.Model;
using SOLARY.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SOLARY.Services
{
    public class MesureService : BaseApiService, IMesureService
    {
        public MesureService() : base("https://solary.vabre.ch/") { }

        public async Task<List<MesureEnergie>> GetAllMesuresEnergieAsync()
        {
            try
            {
                return await GetAsync<List<MesureEnergie>>("GetAllMesuresEnergie") ?? new List<MesureEnergie>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération des mesures: {ex.Message}");
                return new List<MesureEnergie>();
            }
        }

        public async Task<MesureEnergie?> GetMesureEnergieAsync(int id)
        {
            try
            {
                return await GetAsync<MesureEnergie>($"GetMesureEnergie/{id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération de la mesure {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<MesureEnergie>> GetMesuresForBorneAsync(int borneId)
        {
            try
            {
                var allMesures = await GetAllMesuresEnergieAsync();

                // Debug amélioré
                System.Diagnostics.Debug.WriteLine($"🔍 Total mesures récupérées: {allMesures.Count}");
                System.Diagnostics.Debug.WriteLine($"🎯 Recherche pour borne_id: {borneId}");

                // Afficher quelques exemples de borne_id pour debug
                var borneIds = allMesures.Select(m => m.BorneId).Distinct().ToList();
                System.Diagnostics.Debug.WriteLine($"📋 BorneIds trouvés: [{string.Join(", ", borneIds)}]");

                // Compter les mesures par borne
                var countByBorne = allMesures.GroupBy(m => m.BorneId)
                                           .ToDictionary(g => g.Key, g => g.Count());
                foreach (var kvp in countByBorne)
                {
                    System.Diagnostics.Debug.WriteLine($"   Borne {kvp.Key}: {kvp.Value} mesures");
                }

                var filteredMesures = allMesures
                    .Where(m => m.BorneId == borneId)
                    .OrderByDescending(m => m.MeasureDate)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"✅ Mesures filtrées pour borne {borneId}: {filteredMesures.Count}");

                // Afficher les premières mesures trouvées
                foreach (var mesure in filteredMesures.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"   📊 {mesure.MeasureDate:yyyy-MM-dd HH:mm:ss} - V:{mesure.Voltage} A:{mesure.Current} W:{mesure.Power} B:{mesure.BatteryLevel}%");
                }

                return filteredMesures;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de la récupération des mesures pour la borne {borneId}: {ex.Message}");
                return new List<MesureEnergie>();
            }
        }

        public async Task<MesureEnergie?> GetLatestMesureForBorneAsync(int borneId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Recherche de la dernière mesure pour borne_id: {borneId}");

                var mesuresForBorne = await GetMesuresForBorneAsync(borneId);

                System.Diagnostics.Debug.WriteLine($"📊 Nombre de mesures trouvées: {mesuresForBorne.Count}");

                var latestMesure = mesuresForBorne.FirstOrDefault();

                if (latestMesure != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Dernière mesure trouvée:");
                    System.Diagnostics.Debug.WriteLine($"   📅 Date: {latestMesure.MeasureDate:yyyy-MM-dd HH:mm:ss}");
                    System.Diagnostics.Debug.WriteLine($"   ⚡ Voltage: {latestMesure.Voltage}V");
                    System.Diagnostics.Debug.WriteLine($"   🔌 Current: {latestMesure.Current}A");
                    System.Diagnostics.Debug.WriteLine($"   💡 Power: {latestMesure.Power}W");
                    System.Diagnostics.Debug.WriteLine($"   🔋 Battery: {latestMesure.BatteryLevel}%");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Aucune mesure trouvée pour borne_id: {borneId}");
                }

                return latestMesure;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de la récupération de la dernière mesure pour la borne {borneId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<MesureEnergie>> GetMesuresHistoryForBorneAsync(int borneId, int limitCount = 20)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Récupération historique pour borne {borneId}, limite: {limitCount}");

                var allMesures = await GetAllMesuresEnergieAsync();

                // Filtrer par borne_id d'abord
                var mesuresForBorne = allMesures
                    .Where(m => m.BorneId == borneId)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"📊 {mesuresForBorne.Count} mesures totales trouvées pour la borne {borneId}");

                if (mesuresForBorne.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Aucune donnée réelle trouvée, génération de données simulées");
                    return GenerateSimulatedHistory(borneId, limitCount);
                }

                // Trouver la date de la mesure la plus récente
                var latestDate = mesuresForBorne.Max(m => m.MeasureDate);
                System.Diagnostics.Debug.WriteLine($"📅 Dernière mesure datée du: {latestDate:yyyy-MM-dd HH:mm:ss}");

                // Prendre les mesures des dernières 24h à partir de la dernière mesure
                var cutoffDate = latestDate.AddHours(-24);
                System.Diagnostics.Debug.WriteLine($"🕐 Récupération des mesures depuis: {cutoffDate:yyyy-MM-dd HH:mm:ss}");

                var recentMesures = mesuresForBorne
                    .Where(m => m.MeasureDate >= cutoffDate)
                    .OrderBy(m => m.MeasureDate) // Tri chronologique pour l'affichage
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"✅ {recentMesures.Count} mesures récupérées pour les dernières 24h");

                // Afficher quelques exemples pour debug
                foreach (var mesure in recentMesures.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"   📊 {mesure.MeasureDate:yyyy-MM-dd HH:mm:ss} - V:{mesure.Voltage} A:{mesure.Current} W:{mesure.Power} B:{mesure.BatteryLevel}%");
                }

                if (recentMesures.Count > 0)
                {
                    var lastMesure = recentMesures.Last();
                    System.Diagnostics.Debug.WriteLine($"   📊 Dernière: {lastMesure.MeasureDate:yyyy-MM-dd HH:mm:ss} - V:{lastMesure.Voltage} A:{lastMesure.Current} W:{lastMesure.Power} B:{lastMesure.BatteryLevel}%");
                }

                return recentMesures;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetMesuresHistoryForBorneAsync: {ex.Message}");

                // Retourner des données simulées en cas d'erreur
                return GenerateSimulatedHistory(borneId, limitCount);
            }
        }

        private List<MesureEnergie> GenerateSimulatedHistory(int borneId, int count)
        {
            System.Diagnostics.Debug.WriteLine($"🎲 Génération de {count} mesures simulées pour la borne {borneId}");

            var mesures = new List<MesureEnergie>();
            var random = new Random();
            var baseTime = DateTime.Now.AddHours(-2); // Commencer il y a 2 heures

            for (int i = 0; i < count; i++)
            {
                var voltage = 4.8 + random.NextDouble() * 0.4; // 4.8-5.2V
                var current = 0.5 + random.NextDouble() * 2.0; // 0.5-2.5A
                var power = voltage * current; // Calcul réaliste de la puissance

                mesures.Add(new MesureEnergie
                {
                    MesureId = i + 1000, // ID simulé
                    BorneId = borneId,
                    Voltage = Math.Round(voltage, 2),
                    Current = Math.Round(current, 2),
                    Power = Math.Round(power, 2),
                    BatteryLevel = 75 + random.Next(0, 15), // 75-90%
                    TotalEnergy = 100 + random.NextDouble() * 50,
                    SolarPower = power * 0.8, // 80% de la puissance totale
                    EnergyGeneratedKwh = 0.1 + random.NextDouble() * 0.5,
                    EnergyConsumedKwh = 0.05 + random.NextDouble() * 0.3,
                    MeasureDate = baseTime.AddMinutes(i * 6) // Toutes les 6 minutes
                });
            }

            System.Diagnostics.Debug.WriteLine($"✅ {mesures.Count} mesures simulées générées");
            return mesures;
        }

        public async Task<bool> AddMesureEnergieAsync(MesureEnergie mesure)
        {
            try
            {
                var data = new
                {
                    borne_id = mesure.BorneId,
                    voltage = mesure.Voltage,
                    current = mesure.Current,
                    power = mesure.Power,
                    battery_level = mesure.BatteryLevel,
                    total_energy = mesure.TotalEnergy,
                    solar_power = mesure.SolarPower,
                    energy_generated_kwh = mesure.EnergyGeneratedKwh,
                    energy_consumed_kwh = mesure.EnergyConsumedKwh,
                    measure_date = mesure.MeasureDate.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var result = await PostAsync<object>("AddMesureEnergie", data);
                return result != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'ajout de la mesure: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateMesureEnergieAsync(int id, MesureEnergie mesure)
        {
            try
            {
                var data = new
                {
                    borne_id = mesure.BorneId,
                    voltage = mesure.Voltage,
                    current = mesure.Current,
                    power = mesure.Power,
                    battery_level = mesure.BatteryLevel,
                    total_energy = mesure.TotalEnergy,
                    solar_power = mesure.SolarPower,
                    energy_generated_kwh = mesure.EnergyGeneratedKwh,
                    energy_consumed_kwh = mesure.EnergyConsumedKwh,
                    measure_date = mesure.MeasureDate.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var result = await PutAsync<object>($"UpdateMesureEnergie/{id}", data);
                return result != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour de la mesure {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteMesureEnergieAsync(int id)
        {
            try
            {
                var result = await DeleteAsync<object>($"DeleteMesureEnergie/{id}");
                return result != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la suppression de la mesure {id}: {ex.Message}");
                return false;
            }
        }
    }
}
