using System;
using System.Text.Json.Serialization;

namespace SOLARY.Model
{
    public class MesureEnergie
    {
        [JsonPropertyName("mesure_id")]
        public int MesureId { get; set; }

        [JsonPropertyName("borne_id")]
        public int BorneId { get; set; }

        [JsonPropertyName("voltage")]
        public double Voltage { get; set; }

        [JsonPropertyName("current")]
        public double Current { get; set; }

        [JsonPropertyName("power")]
        public double Power { get; set; }

        [JsonPropertyName("battery_level")]
        public int BatteryLevel { get; set; }

        [JsonPropertyName("total_energy")]
        public double? TotalEnergy { get; set; }

        [JsonPropertyName("solar_power")]
        public double? SolarPower { get; set; }

        [JsonPropertyName("energy_generated_kwh")]
        public double? EnergyGeneratedKwh { get; set; }

        [JsonPropertyName("energy_consumed_kwh")]
        public double? EnergyConsumedKwh { get; set; }

        [JsonPropertyName("measure_date")]
        public DateTime MeasureDate { get; set; }

        // Propriété calculée pour la réduction CO2
        public double Co2Reduction => CalculateCo2Reduction();

        private double CalculateCo2Reduction()
        {
            // Formule améliorée pour une borne photovoltaïque
            // Calcul sur une base horaire pour des valeurs plus représentatives
            // Facteur d'émission CO2 du réseau électrique français : ~57g CO2/kWh (2023)
            // Conversion en mg pour des valeurs plus lisibles

            if (Power <= 0) return 0;

            // Calcul de l'énergie produite par heure (en kWh)
            double energieKwhParHeure = Power / 1000.0; // Conversion W → kWh/h

            // CO2 évité par heure en grammes (facteur français plus réaliste)
            double co2EviteGrammesParHeure = energieKwhParHeure * 57.0; // 57g CO2/kWh

            // Conversion en milligrammes pour affichage plus lisible
            double co2EviteMgParHeure = co2EviteGrammesParHeure * 1000.0;

            // Pour l'affichage, on peut diviser par 60 pour avoir une valeur par minute
            // ou garder la valeur horaire selon les préférences
            double co2EviteMgParMinute = co2EviteMgParHeure / 60.0;

            // Debug du calcul
            System.Diagnostics.Debug.WriteLine($"🔍 Calcul CO2 amélioré pour {Power}W:");
            System.Diagnostics.Debug.WriteLine($"   Énergie/h: {energieKwhParHeure:F4} kWh");
            System.Diagnostics.Debug.WriteLine($"   CO2 évité/h: {co2EviteGrammesParHeure:F2}g");
            System.Diagnostics.Debug.WriteLine($"   CO2 évité/min: {co2EviteMgParMinute:F1}mg");

            // Retourner la valeur par minute en mg, arrondie à 1 décimale
            return Math.Round(co2EviteMgParMinute, 1);
        }
    }
}
