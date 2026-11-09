using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;
public class Product
{
    public int ProductID { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên laptop")]
    [MaxLength(200, ErrorMessage = "Tên laptop không được quá 200 ký tự")]
    public string Name { get; set; } = "";

    [Range(1000, 1000000000, ErrorMessage = "Giá bán phải từ 1.000 đến 1.000.000.000 đồng")]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hãng laptop")]
    public int CategoryID { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả sản phẩm")]
    [MaxLength(3000, ErrorMessage = "Mô tả không được quá 3000 ký tự")]
    public string Description { get; set; } = "";

    [MaxLength(500, ErrorMessage = "Đường dẫn ảnh quá dài")]
    public string? ImageUrl { get; set; } = "/images/laptop-default.svg";

    [Range(0, 100000, ErrorMessage = "Tồn kho phải từ 0 đến 100.000 chiếc")]
    public int Stock { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập CPU")]
    [MaxLength(150, ErrorMessage = "Thông tin CPU quá dài")]
    public string Cpu { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập RAM")]
    [MaxLength(80, ErrorMessage = "Thông tin RAM quá dài")]
    public string Ram { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập ổ cứng")]
    [MaxLength(80, ErrorMessage = "Thông tin ổ cứng quá dài")]
    public string Storage { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập card đồ họa")]
    [MaxLength(120, ErrorMessage = "Thông tin card đồ họa quá dài")]
    public string Graphics { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập màn hình")]
    [MaxLength(130, ErrorMessage = "Thông tin màn hình quá dài")]
    public string Screen { get; set; } = "";

    public Category? Category { get; set; }
}
