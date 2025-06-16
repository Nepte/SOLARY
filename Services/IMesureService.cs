using SOLARY.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOLARY.Services
{
    public interface IMesureService
    {
        Task<List<MesureEnergie>> GetAllMesuresEnergieAsync();
        Task<MesureEnergie?> GetMesureEnergieAsync(int id);
        Task<List<MesureEnergie>> GetMesuresForBorneAsync(int borneId);
        Task<MesureEnergie?> GetLatestMesureForBorneAsync(int borneId);
        Task<List<MesureEnergie>> GetMesuresHistoryForBorneAsync(int borneId, int limitCount = 20);
        Task<bool> AddMesureEnergieAsync(MesureEnergie mesure);
        Task<bool> UpdateMesureEnergieAsync(int id, MesureEnergie mesure);
        Task<bool> DeleteMesureEnergieAsync(int id);
    }
}
