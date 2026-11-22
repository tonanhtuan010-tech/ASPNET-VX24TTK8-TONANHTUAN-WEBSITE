using System.ComponentModel.DataAnnotations;

namespace WebsiteBanLaptop.Models;
public class StaffTask
{
    public int StaffTaskID { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên công việc"), MaxLength(150, ErrorMessage = "Tên công việc không được quá 150 ký tự")] public string Title { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận"), MaxLength(100, ErrorMessage = "Tên người nhận không được quá 100 ký tự")] public string AssignedTo { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; } = "";
    public DateTime? Deadline { get; set; }
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
