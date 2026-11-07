using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
