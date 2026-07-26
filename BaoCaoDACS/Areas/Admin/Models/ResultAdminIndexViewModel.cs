namespace BaoCaoDACS.Areas.Admin.Models
{
    public class ResultAdminIndexViewModel
    {
        public string? SearchValue { get; init; }
        public int TotalResults { get; init; }
        public IReadOnlyList<ResultTournamentGroupViewModel> TournamentGroups { get; init; }
            = Array.Empty<ResultTournamentGroupViewModel>();
    }

    public class ResultTournamentGroupViewModel
    {
        public int? TournamentId { get; init; }
        public string TournamentName { get; init; } = string.Empty;
        public int ResultCount { get; init; }
    }
}
