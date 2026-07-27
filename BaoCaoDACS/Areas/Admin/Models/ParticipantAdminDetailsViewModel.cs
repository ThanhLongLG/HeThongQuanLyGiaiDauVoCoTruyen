using System.ComponentModel.DataAnnotations;
using BaoCaoDACS.Models;

namespace BaoCaoDACS.Areas.Admin.Models
{
    public class ParticipantAdminDetailsViewModel
    {
        public Participant Participant { get; set; } = new();
        public List<ParticipantRankingEditViewModel> Rankings { get; set; } = new();
    }

    public class ParticipantRankingEditViewModel
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public string? TournamentName { get; set; }

        [Range(0, 5000, ErrorMessage = "Điểm ranking phải nằm trong khoảng từ 0 đến 5000.")]
        public float Rating { get; set; }

        public int Tier { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
