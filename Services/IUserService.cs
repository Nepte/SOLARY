using System.Threading.Tasks;
using SOLARY.Model;

namespace SOLARY.Services
{
    public interface IUserService
    {
        Task<User?> GetUserById(int userId);
        Task<User?> GetUserByEmail(string email); // Nouvelle méthode
        Task<bool> UpdateLockerCode(int userId, string newCode);
        Task<bool> ValidateLockerCode(int userId, string code);
    }
}
