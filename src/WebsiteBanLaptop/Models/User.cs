using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;

public class User
{
    public int UserID { get; set; }
    [Required, MaxLength(100)]
    public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer";
}
