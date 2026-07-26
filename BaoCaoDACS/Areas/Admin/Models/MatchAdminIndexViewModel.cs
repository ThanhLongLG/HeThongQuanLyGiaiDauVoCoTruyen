namespace BaoCaoDACS.Areas.Admin.Models
{
    public class MatchAdminIndexViewModel
    {
        public string? SearchValue { get; init; }
        public int TotalMatches { get; init; }
        public IReadOnlyList<MatchTournamentGroupViewModel> TournamentGroups { get; init; }
            = Array.Empty<MatchTournamentGroupViewModel>();
    }

    public class MatchTournamentGroupViewModel
    {
        public int? TournamentId { get; init; }
        public string TournamentName { get; init; } = string.Empty;
        public int MatchCount { get; init; }
    }
}
