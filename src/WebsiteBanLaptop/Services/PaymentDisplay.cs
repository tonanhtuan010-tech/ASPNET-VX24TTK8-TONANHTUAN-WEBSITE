namespace WebsiteBanLaptop.Services;

public static class PaymentDisplay
{
    public static string Method(string method) => method switch
    {
        "COD" => "Thanh toán khi nhận hàng (COD)",
        "VNPAY" => "VNPay",
        "MOMO" => "Ví MoMo",
        _ => "Khác"
    };

    public static string Status(string status) => status switch
    {
        "Unpaid" => "Chưa thanh toán",
        "Pending" => "Chờ xác nhận thanh toán",
        "Paid" => "Đã thanh toán",
        "Failed" => "Thanh toán thất bại",
        _ => "Đang xử lý"
    };
}
