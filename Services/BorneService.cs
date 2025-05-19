using SOLARY.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOLARY.Services
{
    public class BorneService : BaseApiService
    {
        public BorneService() : base("https://solary.vabre.ch/")
        {
        }

        // Récupérer toutes les bornes
        public async Task<List<Borne>?> GetAllBornes()
        {
            return await GetAsync<List<Borne>>("GetAllBornes");
        }

        // Récupérer une borne par son ID
        public async Task<Borne?> GetBorne(int id)
        {
            return await GetAsync<Borne>($"GetBorne/{id}");
        }

        // Récupérer les bornes à proximité
        public async Task<List<Borne>?> GetNearbyBornes(double latitude, double longitude, double radius = 10)
        {
            return await GetAsync<List<Borne>>($"GetNearbyBornes?lat={latitude}&lng={longitude}&radius={radius}");
        }

        // Ajouter une borne
        public async Task<ApiResponse?> AddBorne(Borne borne)
        {
            return await PostAsync<ApiResponse>("AddBorne", borne);
        }

        // Mettre à jour une borne
        public async Task<ApiResponse?> UpdateBorne(int id, Borne borne)
        {
            return await PutAsync<ApiResponse>($"UpdateBorne/{id}", borne);
        }

        // Mettre à jour le statut d'une borne
        public async Task<ApiResponse?> UpdateBorneStatus(int id, string status, int? chargePercentage = null)
        {
            var data = new
            {
                status,
                charge_percentage = chargePercentage
            };

            return await PutAsync<ApiResponse>($"UpdateBorneStatus/{id}", data);
        }

        // Supprimer une borne
        public async Task<ApiResponse?> DeleteBorne(int id)
        {
            return await DeleteAsync<ApiResponse>($"DeleteBorne/{id}");
        }
    }
}
