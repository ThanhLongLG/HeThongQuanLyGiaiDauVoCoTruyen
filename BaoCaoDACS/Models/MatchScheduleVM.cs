using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BAO_CAO.Models; // ApplicationUser namespace của bạn

namespace BaoCaoDACS.Models
{
    public class MatchScheduleVM
    {
        public string MatchId { get; set; }

        public string FighterAName { get; set; }
        public string FighterBName { get; set; }

        public float FighterAWinPercent { get; set; }
        public float FighterBWinPercent => 100 - FighterAWinPercent;

        public DateTime Date { get; set; }
        public string VongDau { get; set; }
        public string HangCan { get; set; }
        public string SanDau { get; set; }
    }

}
