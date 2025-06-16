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
                return allMesures
                    .Where(m => m.BorneId == borneId)
                    .OrderByDescending(m => m.MeasureDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération des mesures pour la borne {borneId}: {ex.Message}");
                return new List<MesureEnergie>();
            }
        }

        public async Task<MesureEnergie?> GetLatestMesureForBorneAsync(int borneId)
        {
            try
            {
                var mesuresForBorne = await GetMesuresForBorneAsync(borneId);
                return mesuresForBorne.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération de la dernière mesure pour la borne {borneId}: {ex.Message}");
                return null;
            }
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
