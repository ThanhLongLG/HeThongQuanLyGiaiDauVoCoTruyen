using System.Drawing.Printing;
using BaoCaoDACS.Models;
using BaoCaoDACS.Models;
using Microsoft.EntityFrameworkCore;

namespace BaoCaoDACS.Reponsitory
{
    public class EFLoaiHinheponsitory : ILoaiHinhreponsitory
    {

        private readonly AppDbContext _context;

        public EFLoaiHinheponsitory(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<LoaiHinhThiDau>> GetAllAsync(string? searchValue)
        {
            var query = _context.loaiHinhThiDau.AsQueryable();

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(c => c.Name.Contains(searchValue));
            }

            return await query.ToListAsync();
        }
        public async Task<IEnumerable<LoaiHinhThiDau>> GetAllAsync()
        {
            return await _context.loaiHinhThiDau.ToListAsync();
        }

        public async Task<LoaiHinhThiDau> GetByIdAsync(int ID)
        {
            return await _context.loaiHinhThiDau.FirstOrDefaultAsync(kh => kh.LoaiHinhThiDauId == ID);
        }

    }
}
