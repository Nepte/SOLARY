using SOLARY.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOLARY.Services
{
    public interface ICasierService
    {
        // === MÉTHODES BASÉES SUR L'API EXISTANTE ===
        Task<List<Casier>?> GetAllCasiers();
        Task<ApiResponse?> AddCasier(Casier casier);
        Task<ApiResponse?> UpdateCasier(int casierId, Casier casier);
        Task<ApiResponse?> DeleteCasier(int casierId);

        // === MÉTHODES UTILITAIRES ===
        Task<List<Casier>?> GetCasiersByBorneId(int borneId);
        Task<Casier?> GetCasier(int casierId);
        Task<List<Casier>?> GetCasiersByUserId(int userId);
        Task<List<Casier>?> GetCasiersLibresByBorne(int borneId);

        // === ACTIONS SUR LES CASIERS ===
        Task<ApiResponse?> ReserverCasier(int casierId, int userId);
        Task<ApiResponse?> LibererCasier(int casierId);
        Task<ApiResponse?> OccuperCasier(int casierId, int userId);
        Task<ApiResponse?> AnnulerReservation(int casierId, int userId);
        Task<ApiResponse?> UpdateCasierStatus(int casierId, string status, int? userId = null);

        // === UTILITAIRES ===
        Task<bool> IsCasierAvailable(int casierId);
        Task<Casier?> GetReservationActive(int userId);
        Task<ApiResponse?> CreateCasierForBorne(int borneId);

        // Ajouter ces méthodes à l'interface
        Task<List<Casier>?> GetCasiersByBorneIdAsync(int borneId);
        Task<bool> ReserverCasierAsync(int casierId, int userId);
        Task<bool> AnnulerReservationAsync(int casierId, int userId);
    }
}
