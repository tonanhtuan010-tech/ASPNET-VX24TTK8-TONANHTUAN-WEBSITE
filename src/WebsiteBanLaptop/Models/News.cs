using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;
public class News
{
    public int NewsID { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề tin tức"), MaxLength(200, ErrorMessage = "Tiêu đề không được quá 200 ký tự")] public string Title { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập nội dung tin tức"), MaxLength(4000, ErrorMessage = "Nội dung không được quá 4000 ký tự")] public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
