using BaoCaoDACS.Areas.Admin.Models;
using BaoCaoDACS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaoCaoDACS.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class NewsController : Controller
{
    private const long MaxImageSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NewsController> _logger;

    public NewsController(
        AppDbContext context,
        IWebHostEnvironment environment,
        ILogger<NewsController> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.News.AsNoTracking();

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

    public async Task<IActionResult> Details(int id)
    {
        var news = await _context.News.AsNoTracking().FirstOrDefaultAsync(item => item.NewsId == id);
        return news == null ? NotFound() : View(news);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new NewsFormViewModel
        {
            PublishedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddDays(30),
            IsPublished = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NewsFormViewModel model)
    {
        ValidateDates(model);
        ValidateImage(model.ImageFile, imageRequired: true);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? imageUrl = null;
        try
        {
            imageUrl = await SaveImageAsync(model.ImageFile!);
            var news = new News
            {
                Title = model.Title.Trim(),
                Summary = model.Summary?.Trim(),
                Content = model.Content.Trim(),
                ImageUrl = imageUrl,
                PublishedAt = model.PublishedAt,
                ExpiresAt = model.ExpiresAt,
                IsPublished = model.IsPublished,
                CreatedAt = DateTime.Now
            };

            _context.News.Add(news);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã tạo tin “{news.Title}”.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            DeleteImage(imageUrl);
            _logger.LogError(ex, "Không thể tạo tin tức.");
            ModelState.AddModelError(string.Empty, "Không thể lưu tin tức. Vui lòng thử lại.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var news = await _context.News.AsNoTracking().FirstOrDefaultAsync(item => item.NewsId == id);
        if (news == null)
        {
            return NotFound();
        }

        return View(new NewsFormViewModel
        {
            NewsId = news.NewsId,
            Title = news.Title,
            Summary = news.Summary,
            Content = news.Content,
            PublishedAt = news.PublishedAt,
            ExpiresAt = news.ExpiresAt,
            IsPublished = news.IsPublished,
            ExistingImageUrl = news.ImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NewsFormViewModel model)
    {
        if (id != model.NewsId)
        {
            return NotFound();
        }

        ValidateDates(model);
        ValidateImage(model.ImageFile, imageRequired: false);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var news = await _context.News.FirstOrDefaultAsync(item => item.NewsId == id);
        if (news == null)
        {
            return NotFound();
        }

        string? newImageUrl = null;
        var oldImageUrl = news.ImageUrl;
        try
        {
            if (model.ImageFile is { Length: > 0 })
            {
                newImageUrl = await SaveImageAsync(model.ImageFile);
                news.ImageUrl = newImageUrl;
            }

            news.Title = model.Title.Trim();
            news.Summary = model.Summary?.Trim();
            news.Content = model.Content.Trim();
            news.PublishedAt = model.PublishedAt;
            news.ExpiresAt = model.ExpiresAt;
            news.IsPublished = model.IsPublished;
            news.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            if (newImageUrl != null)
            {
                DeleteImage(oldImageUrl);
            }

            TempData["SuccessMessage"] = $"Đã cập nhật tin “{news.Title}”.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            DeleteImage(newImageUrl);
            _logger.LogError(ex, "Không thể cập nhật tin tức {NewsId}.", id);
            model.ExistingImageUrl = oldImageUrl;
            ModelState.AddModelError(string.Empty, "Không thể cập nhật tin tức. Vui lòng thử lại.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var news = await _context.News.AsNoTracking().FirstOrDefaultAsync(item => item.NewsId == id);
        return news == null ? NotFound() : View(news);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var news = await _context.News.FirstOrDefaultAsync(item => item.NewsId == id);
        if (news == null)
        {
            return NotFound();
        }

        try
        {
            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            DeleteImage(news.ImageUrl);
            TempData["SuccessMessage"] = $"Đã xóa tin “{news.Title}”.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể xóa tin tức {NewsId}.", id);
            ModelState.AddModelError(string.Empty, "Không thể xóa tin tức. Vui lòng thử lại.");
            return View(news);
        }
    }

    private void ValidateDates(NewsFormViewModel model)
    {
        if (model.ExpiresAt.HasValue && model.ExpiresAt.Value <= model.PublishedAt)
        {
            ModelState.AddModelError(nameof(model.ExpiresAt), "Ngày hết hạn phải sau ngày đăng.");
        }
    }

    private void ValidateImage(IFormFile? image, bool imageRequired)
    {
        if (image == null || image.Length == 0)
        {
            if (imageRequired)
            {
                ModelState.AddModelError(nameof(NewsFormViewModel.ImageFile), "Vui lòng chọn ảnh đại diện.");
            }
            return;
        }

        var extension = Path.GetExtension(image.FileName);
        if (!AllowedImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(
                nameof(NewsFormViewModel.ImageFile),
                "Ảnh phải có định dạng JPG, PNG, WEBP hoặc GIF.");
        }

        if (image.Length > MaxImageSize)
        {
            ModelState.AddModelError(nameof(NewsFormViewModel.ImageFile), "Kích thước ảnh không được vượt quá 5 MB.");
        }
    }

    private async Task<string> SaveImageAsync(IFormFile image)
    {
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "news");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        try
        {
            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await image.CopyToAsync(stream);
        }
        catch
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            throw;
        }

        return $"/images/news/{fileName}";
    }

    private void DeleteImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) ||
            !imageUrl.StartsWith("/images/news/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(_environment.WebRootPath, "images", "news", fileName);
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể xóa ảnh tin tức {ImagePath}.", filePath);
        }
    }
}
