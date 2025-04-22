using System.ComponentModel.DataAnnotations;

namespace BaoCaoDACS.Models
{
    public class Result
    {
        public int ResultID { get; set; }
        public int TournamentID { get; set; }
        public Tournament Tournament { get; set; }
        public int ParticipantID { get; set; }
        public Participant Participant { get; set; }
        public string Rank { get; set; }
    }
}