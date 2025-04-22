using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TournamentsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetTournaments()
    {
        var tournaments = new[]
        {
            new { Id = 1, Name = "Giải Vô Địch Quốc Gia", StartDate = "2025-04-20", EndDate = "2025-04-30", Location = "Thái Nguyên", Participants = 150 },
            new { Id = 2, Name = "Giao Lưu Võ Thuật Việt Nam - Triều Tiên", StartDate = "2025-04-22", EndDate = "2025-04-22", Location = "Thái Nguyên", Participants = 50 }
        };

        return Ok(tournaments);
    }
}
