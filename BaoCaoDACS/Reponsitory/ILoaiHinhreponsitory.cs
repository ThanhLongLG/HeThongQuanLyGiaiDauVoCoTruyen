
using BaoCaoDACS.Models;

namespace BaoCaoDACS.Reponsitory
{
    public interface ILoaiHinhreponsitory
    {
        Task<IEnumerable<LoaiHinhThiDau>> GetAllAsync(string? searchValue);
        Task<IEnumerable<LoaiHinhThiDau>> GetAllAsync();
        Task<LoaiHinhThiDau> GetByIdAsync(int ID);
    }
}
