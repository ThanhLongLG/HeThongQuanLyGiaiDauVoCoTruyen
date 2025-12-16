using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaoCaoDACS.Models
{
    public class Socre
    {
        [Key]
        public int ScoreId { get; set; }
       
      
        public float? Diem { get; set; }

        public byte? Kq { get; set; }
        public string? KietQua { get; set; }
        public string? Danhgia { get; set; }



        [ForeignKey("ParticipantId")]
        public String ParticipantId { get; set; }
        public Participant participant { get; set; }

        [ForeignKey("MatchId")]
        public String MatchId { get; set; } 
        public Match match { get; set; } 

    }

}
