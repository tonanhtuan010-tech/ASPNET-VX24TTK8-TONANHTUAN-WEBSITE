using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;
public class Category
{
    public int CategoryID { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên hãng laptop"), MaxLength(60, ErrorMessage = "Tên hãng không được quá 60 ký tự")] public string CategoryName { get; set; } = "";
    public List<Product> Products { get; set; } = new();
}
