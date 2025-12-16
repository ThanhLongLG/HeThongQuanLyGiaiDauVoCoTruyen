using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BAO_CAO.Models;

namespace BaoCaoDACS.Models
{
    public class Participant
    {
        [Key]
        [Column("ParticipantID")] 
        public String ParticipantID { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Club { get; set; }

        public string? sdt { get; set; }

        public string? email { get; set; }

   
        public float? CanNang { get; set; }

  
        public float? ChieuCao { get; set; }

   
        public int? tuoi { get; set; }

        public string? Diachi { get; set; }

        public string? Thanhtoan { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public ApplicationUser? ApplicationUser { get; set; }

        public int? TournamentID { get; set; }

        [ForeignKey("TournamentID")]
        public Tournament? Tournament { get; set; }
        public ICollection<Socre>? Socre { get; set; } = new List<Socre>();

    }
}