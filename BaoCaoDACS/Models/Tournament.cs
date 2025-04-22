using System.ComponentModel.DataAnnotations;

namespace BaoCaoDACS.Models
{
    public class Tournament
    {
        public int TournamentID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        [Required]
        public string Location { get; set; }
        public string Status { get; set; } = "Upcoming";
    }

}