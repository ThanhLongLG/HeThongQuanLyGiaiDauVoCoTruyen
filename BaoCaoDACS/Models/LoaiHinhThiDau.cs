using System.ComponentModel.DataAnnotations;

namespace BaoCaoDACS.Models
{
    public class LoaiHinhThiDau
    {
        [Key]
        public int LoaiHinhThiDauId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string MonVo { get; set; }

        public ICollection<Tournament> tournament { get; set; } = new List<Tournament>();
    }

}