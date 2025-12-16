
using BaoCaoDACS.Models;

namespace BaoCaoDACS.Reponsitory
{
    public interface INguoidungreponsitory
    {
        Task<Participant> GetByUserIdAsync(string userId);
        Task<IEnumerable<Participant>> GetAllAsync(string? searchValue);
        Task<IEnumerable<Participant>> GetAllAsync();
        Task<Participant> GetByIdAsync(string participantId);
        Task AddAsync(Participant Participant);
        Task DeleteAsync(string participantId);
        Task UpdateAsync(Participant khachHang);
        Task<int> GetTotalCountAsync();
    }
}
