using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaoCaoDACS.Models
{
    public class Match
    {
        [Key]
        public string MatchId { get; set; } = "TD_" + DateTime.Now.ToString("yyyyMMdd") + "_" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
        [Required]
        public string Vongdau  { get; set; }
        [Required]
        public string SanDau { get; set; }
        [Required]
        public string? Hangcan { get; set; }
        [Required]
        public string Trongtai { get; set; }
        public int? trangthai { get; set; }
        [Required]
        public DateTime Date { get; set; }

        [ForeignKey("LoaiHinhThiDauId")]
        public int LoaiHinhThiDauId { get; set; }
        public LoaiHinhThiDau? LoaiHinhThiDau { get; set; }

        [ForeignKey("TournamentID")]
        public int? TournamentID { get; set; }
        public Tournament? Tournament { get; set; }
        public ICollection<Socre> Socre { get; set; } = new List<Socre>();


    }

}
