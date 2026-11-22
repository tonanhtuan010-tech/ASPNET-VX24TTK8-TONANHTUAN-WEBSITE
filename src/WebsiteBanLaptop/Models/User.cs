using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebsiteBanLaptop.Models;

public class User : IdentityUser<int>
{
    [NotMapped]
    public int UserID { get => Id; set => Id = value; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = "";

    [MaxLength(300)]
    public string? Address { get; set; }

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Customer";

    public List<Order> Orders { get; set; } = new();
}
