using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;

public class ProductReview
{
    public int ProductReviewID { get; set; }
    public int ProductID { get; set; }
    public int UserID { get; set; }
    [Range(1,5)] public int Rating { get; set; }
    [Required, MinLength(10), MaxLength(800)] public string Comment { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Product? Product { get; set; }
    public User? User { get; set; }
}
