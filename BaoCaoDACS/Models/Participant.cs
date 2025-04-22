using System.ComponentModel.DataAnnotations;

namespace BaoCaoDACS.Models
{
    public class Participant
    {
        public int ParticipantID { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Club { get; set; }
        public int TournamentID { get; set; }
        public Tournament Tournament { get; set; }
        public decimal Score { get; set; } = 0;
    }
}