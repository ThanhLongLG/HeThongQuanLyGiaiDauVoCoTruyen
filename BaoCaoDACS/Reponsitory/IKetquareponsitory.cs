
using BaoCaoDACS.Models;

namespace BaoCaoDACS.Reponsitory
{
    public interface IKetquareponsitory
    {   
        Task<IEnumerable<Socre>> GetAllAsync(string? searchValue);
        Task<IEnumerable<Socre>> GetAllAsync();
        Task<Socre> GetByIdAsync(int Socre);
        Task AddAsync(Socre socre);
        Task DeleteAsync(int SocreId);
        Task UpdateAsync(Socre socre);
        Task<int> GetTotalCountAsync();
    }
}
