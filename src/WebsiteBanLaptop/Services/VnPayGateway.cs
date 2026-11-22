using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Services;

public sealed class VnPayGateway
{
    private readonly PaymentOptions _settings;
    public VnPayGateway(IOptions<PaymentOptions> options) => _settings = options.Value;

    private static string Hmac(string secret, string data)
    {
        using var sha = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }
    private static string Enc(string s) => WebUtility.UrlEncode(s);
    private static string SignText(IEnumerable<KeyValuePair<string, string>> values) =>
        string.Join("&", values.OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => Enc(x.Key) + "=" + Enc(x.Value)));

    public string CreateUrl(Order order, string? clientIp)
    {
        if (!_settings.CanUseVnPay) throw new InvalidOperationException("VNPay chưa được cấu hình.");
        var baseUri = new Uri(_settings.VnPay.PaymentUrl);
        if (baseUri.Scheme != "https" || !baseUri.Host.Equals("pay.vnpay.vn", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Địa chỉ thanh toán VNPay không hợp lệ.");
        if (order.PaymentReference is null || order.TotalAmount <= 0 || order.TotalAmount != decimal.Truncate(order.TotalAmount))
            throw new InvalidOperationException("Đơn hàng chưa hợp lệ.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
        var values = new Dictionary<string,string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0", ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.VnPay.TmnCode, ["vnp_Amount"] = checked((long)(order.TotalAmount * 100)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND", ["vnp_TxnRef"] = order.PaymentReference,
            ["vnp_OrderInfo"] = "Thanh toan don hang " + order.OrderID,
            ["vnp_OrderType"] = "other", ["vnp_Locale"] = "vn",
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_ExpireDate"] = TimeZoneInfo.ConvertTimeFromUtc(order.PaymentExpiresAtUtc!.Value, zone).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_IpAddr"] = string.IsNullOrEmpty(clientIp) || !System.Net.IPAddress.TryParse(clientIp, out _) ? "127.0.0.1" : clientIp,
            ["vnp_ReturnUrl"] = _settings.PublicBaseUrl.TrimEnd('/') + "/Payments/Result?reference=" + Uri.EscapeDataString(order.PaymentReference)
        };
        var query = SignText(values);
        return baseUri + "?" + query + "&vnp_SecureHash=" + Hmac(_settings.VnPay.HashSecret, query);
    }

    public bool TryVerify(IQueryCollection query, out PaymentProof? proof)
    {
        proof = null;
        if (!_settings.CanUseVnPay || query.Count > 45 ||
            !query.TryGetValue("vnp_SecureHash", out var hash) || hash.Count != 1 ||
            !query.TryGetValue("vnp_TmnCode", out var code) || code.ToString() != _settings.VnPay.TmnCode)
            return false;
        var values = new Dictionary<string,string>(StringComparer.Ordinal);
        foreach (var pair in query)
        {
            if (!pair.Key.StartsWith("vnp_", StringComparison.Ordinal) ||
                pair.Key is "vnp_SecureHash" or "vnp_SecureHashType") continue;
            if (pair.Value.Count != 1 || pair.Value.ToString().Length > 500) return false;
            values[pair.Key] = pair.Value.ToString();
        }
        var expected = Hmac(_settings.VnPay.HashSecret, SignText(values));
        var supplied = hash.ToString();
        if (supplied.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(supplied.ToLowerInvariant()))) return false;
        if (!values.TryGetValue("vnp_TxnRef", out var reference) || reference.Length is < 10 or > 100 ||
            !values.TryGetValue("vnp_Amount", out var amountText) ||
            !long.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture, out var amount100) ||
            amount100 <= 0 || amount100 % 100 != 0 ||
            !values.TryGetValue("vnp_TransactionStatus", out var status) ||
            !values.TryGetValue("vnp_ResponseCode", out var response)) return false;
        values.TryGetValue("vnp_TransactionNo", out var transactionId);
        proof = new PaymentProof("VNPAY", reference, amount100 / 100,
            status == "00" && response == "00", transactionId ?? "");
        return true;
    }
}
