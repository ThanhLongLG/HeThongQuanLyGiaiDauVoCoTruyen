using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BaoCaoDACS.Areas.Admin.Models;

public class NewsFormViewModel
{
    public int NewsId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự.")]
    [Display(Name = "Mô tả ngắn")]
    public string? Summary { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Ngày đăng")]
    public DateTime PublishedAt { get; set; } = DateTime.Now;

    [Display(Name = "Ngày hết hạn")]
    public DateTime? ExpiresAt { get; set; }

    [Display(Name = "Hiển thị công khai")]
    public bool IsPublished { get; set; } = true;

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImageUrl { get; set; }
}
