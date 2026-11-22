using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;
public class Order
{
    public int OrderID { get; set; }
    public int UserID { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    [Required, MaxLength(40)] public string Status { get; set; } = "Chờ xác nhận";
    public decimal TotalAmount { get; set; }
    [Required, MaxLength(100)] public string CustomerName { get; set; } = "";
    [Required, MaxLength(20)] public string Phone { get; set; } = "";
    [Required, MaxLength(300)] public string Address { get; set; } = "";
    [MaxLength(500)] public string? Note { get; set; }
    [MaxLength(20)] public string PaymentMethod { get; set; } = "COD";
    [Required, MaxLength(20)] public string PaymentStatus { get; set; } = "Unpaid";
    [MaxLength(100)] public string? PaymentReference { get; set; }
    [MaxLength(100)] public string? PaymentTransactionId { get; set; }
    public DateTime? PaymentUpdatedAtUtc { get; set; }
    public DateTime? PaymentExpiresAtUtc { get; set; }
    public User? User { get; set; }
    public List<OrderDetail> OrderDetails { get; set; } = new();
}
