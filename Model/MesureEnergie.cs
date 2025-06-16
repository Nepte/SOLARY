using System;
using System.Text.Json.Serialization;

namespace SOLARY.Model
{
    public class MesureEnergie
    {
        public int MesureId { get; set; }
        public int BorneId { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Power { get; set; }
        public int BatteryLevel { get; set; }
        public double? TotalEnergy { get; set; }
        public double? SolarPower { get; set; }
        public double? EnergyGeneratedKwh { get; set; }
        public double? EnergyConsumedKwh { get; set; }
        public DateTime MeasureDate { get; set; }

        // Propriété calculée pour la réduction CO2
        public double Co2Reduction => CalculateCo2Reduction();

        private double CalculateCo2Reduction()
        {
            // Formule exacte selon ChatGPT :
            // Énergie (kWh) = Puissance (W) × Durée (h) / 1000
            // Durée = 30 secondes = 0.00833 h
            // CO2 évité = Énergie (kWh) × 400 g/kWh

            double dureeHeures = 30.0 / 3600.0; // 30 secondes = 0.00833 h
            double energieWh = Power * dureeHeures; // En Wh (pas encore en kWh)
            double energieKwh = energieWh / 1000.0; // Conversion Wh → kWh
            double co2EviteGrammes = energieKwh * 400.0; // 400g CO2/kWh

            // Exemple avec 1.4W :
            // energieWh = 1.4 × 0.00833 = 0.01166 Wh
            // energieKwh = 0.01166 / 1000 = 0.00001166 kWh  
            // co2EviteGrammes = 0.00001166 × 400 = 0.004664g

            // Debug du calcul étape par étape
            System.Diagnostics.Debug.WriteLine($"🔍 Calcul CO2 pour {Power}W:");
            System.Diagnostics.Debug.WriteLine($"   Durée: {dureeHeures}h");
            System.Diagnostics.Debug.WriteLine($"   Énergie Wh: {energieWh}");
            System.Diagnostics.Debug.WriteLine($"   Énergie kWh: {energieKwh}");
            System.Diagnostics.Debug.WriteLine($"   CO2 évité: {co2EviteGrammes}g");

            return Math.Round(co2EviteGrammes, 3); // 6 décimales pour les très petites valeurs
        }
    }
}
