
using BaoCaoDACS.Models;

namespace BaoCaoDACS.Reponsitory
{
    public interface ITranDaureponsitory
    {
   
        Task<IEnumerable<Match>> GetAllAsync(string? searchValue);
        Task<IEnumerable<Match>> GetAllAsync();
        Task<Match> GetByIdAsync(string MatchId);
        Task AddAsync(Match match);
        Task DeleteAsync(string MatchId);
        Task UpdateAsync(Match khachHang);
        Task<int> GetTotalCountAsync();
    }
}
