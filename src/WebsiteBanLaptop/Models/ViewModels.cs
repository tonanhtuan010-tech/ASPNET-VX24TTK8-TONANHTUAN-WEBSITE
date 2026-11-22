using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;
public class RegisterViewModel
{
    [Required(ErrorMessage = "Bạn chưa nhập họ tên"), MaxLength(100)]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ"), MaxLength(150)] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")] public string Password { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu"), Compare(nameof(Password), ErrorMessage = "Mật khẩu không giống nhau")]
    public string ConfirmPassword { get; set; } = "";
}
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ"), MaxLength(150)] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public sealed class AdminChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Mật khẩu mới phải có từ 10 đến 128 ký tự")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = "";
}
public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    [RegularExpression("^(COD|VNPAY|MOMO)$", ErrorMessage = "Phương thức thanh toán không hợp lệ")]
    public string PaymentMethod { get; set; } = "COD";
    [Required(ErrorMessage = "Bạn chưa nhập họ tên"), MaxLength(100)]
    public string CustomerName { get; set; } = "";
    [Required, RegularExpression(@"^[0-9+ .-]{9,20}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = "";
    [Required(ErrorMessage = "Bạn chưa nhập địa chỉ"), MaxLength(300)]
    public string Address { get; set; } = "";
    [MaxLength(500, ErrorMessage = "Ghi chú không được quá 500 ký tự")] public string? Note { get; set; }
}
public class CartItem
{
    public int ProductID { get; set; }
    public int Quantity { get; set; }
}
public class CartLine
{
    public Product Product { get; set; } = new();
    public int Quantity { get; set; }
    public decimal Amount => Product.Price * Quantity;
}
