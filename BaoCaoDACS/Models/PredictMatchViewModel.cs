using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BaoCaoDACS.Models
{
    public class PredictMatchViewModel
    {
        public int? SelectedTournamentId { get; set; }
        public string? SelectedMatchId { get; set; }

        public List<SelectListItem> Tournaments { get; set; } = new();
        public List<SelectListItem> Matches { get; set; } = new();

        public string? FighterAName { get; set; }
        public string? FighterBName { get; set; }
        public float? FighterAProbPercent { get; set; }
        public float? FighterBProbPercent { get; set; }
        public string? WinnerName { get; set; }
    }
}