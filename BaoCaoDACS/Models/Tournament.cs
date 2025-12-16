using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaoCaoDACS.Models
{
    public class Tournament
    {
        [Key]
        public int TournamentID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        [Required]
        public string Location { get; set; }
        [Required]
        public string HinhThucThiDau { get; set; }

        [Required]
        public string DoiTuongThamGia { get; set; }

        [Required]
        public string QuyMoiaiDa { get; set; }

        [Required]
        public string BanToChuc { get; set; }

        public int Phithamgia { get; set; }

        [Required]
        public string Status { get; set; } = "Upcoming";

        public string? ImageUrl { get; set; }

        public List<string>? ImageUrls { get; set; }

        [ForeignKey("LoaiHinhThiDauId")]
        public int LoaiHinhThiDauId { get; set; }
        public LoaiHinhThiDau? LoaiHinhThiDau { get; set; }


        public ICollection<Match> match { get; set; } = new List<Match>();
        public ICollection<Participant> participant { get; set; } = new List<Participant>();
        public ICollection<TournamentRanking> TournamentRankings { get; set; } = new List<TournamentRanking>();

    }

}
