using BaoCaoDACS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaoCaoDACS.Controllers;

[Route("News")]
public class NewsController : Controller
{
    private readonly AppDbContext _context;

    public NewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search)
    {
        var now = DateTime.Now;
        var query = _context.News
            .AsNoTracking()
            .Where(item =>
                item.IsPublished &&
                item.PublishedAt <= now &&
                (!item.ExpiresAt.HasValue || item.ExpiresAt.Value >= now));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(item =>
                item.Title.Contains(keyword) ||
                (item.Summary != null && item.Summary.Contains(keyword)));
            ViewBag.Search = keyword;
        }

        var news = await query
            .OrderByDescending(item => item.PublishedAt)
            .ThenByDescending(item => item.NewsId)
            .ToListAsync();

        return View(news);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var now = DateTime.Now;
        var news = await _context.News
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.NewsId == id &&
                item.IsPublished &&
                item.PublishedAt <= now &&
                (!item.ExpiresAt.HasValue || item.ExpiresAt.Value >= now));

        return news == null ? NotFound() : View(news);
    }
}
