using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Services;

public sealed class MoMoGateway
{
    private readonly PaymentOptions _settings;
    private readonly HttpClient _http;
    public MoMoGateway(IOptions<PaymentOptions> options, HttpClient http)
    {
        _settings = options.Value;
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(35);
    }

    private static string Hmac(string secret, string text)
    {
        using var sha = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }
    public async Task<string> CreateUrlAsync(Order order, CancellationToken token)
    {
        if (!_settings.CanUseMoMo) throw new InvalidOperationException("MoMo chưa được cấu hình.");
        if (!Uri.TryCreate(_settings.MoMo.CreateUrl, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != "https" || endpoint.Host != "payment.momo.vn")
            throw new InvalidOperationException("Địa chỉ kết nối MoMo không hợp lệ.");
        if (order.PaymentReference == null || order.TotalAmount <= 0 || order.TotalAmount != decimal.Truncate(order.TotalAmount) ||
            order.TotalAmount > 50000000)
            throw new InvalidOperationException("Số tiền thanh toán không hợp lệ.");
        var reference = order.PaymentReference;
        var amount = ((long)order.TotalAmount).ToString(CultureInfo.InvariantCulture);
        var info = "Thanh toan don hang " + order.OrderID;
        var baseUrl = _settings.PublicBaseUrl.TrimEnd('/');
        var redirectUrl = baseUrl + "/Payments/Result?reference=" + Uri.EscapeDataString(reference);
        var ipnUrl = baseUrl + "/Payments/MoMoIpn";
        var partner = _settings.MoMo.PartnerCode;
        var access = _settings.MoMo.AccessKey;
        var raw = $"accessKey={access}&amount={amount}&extraData=&ipnUrl={ipnUrl}&orderId={reference}&orderInfo={info}&partnerCode={partner}&redirectUrl={redirectUrl}&requestId={reference}&requestType=captureWallet";
        var body = new
        {
            partnerCode = partner, requestId = reference, amount = long.Parse(amount, CultureInfo.InvariantCulture),
            orderId = reference, orderInfo = info, redirectUrl, ipnUrl,
            requestType = "captureWallet", extraData = "", lang = "vi",
            signature = Hmac(_settings.MoMo.SecretKey, raw)
        };
        using var response = await _http.PostAsJsonAsync(endpoint, body, token);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
        var obj = json.RootElement;
        if (!obj.TryGetProperty("resultCode", out var status) || status.GetInt32() != 0 ||
            !obj.TryGetProperty("payUrl", out var urlElement))
            throw new InvalidOperationException("Cổng MoMo chưa thể tạo giao dịch.");
        var url = urlElement.GetString() ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var paymentUrl) ||
            paymentUrl.Scheme != "https" ||
            !(paymentUrl.Host.Equals("momo.vn", StringComparison.OrdinalIgnoreCase) ||
            paymentUrl.Host.EndsWith(".momo.vn", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("MoMo trả về địa chỉ không hợp lệ.");
        return url;
    }

    public bool TryVerify(JsonElement json, out PaymentProof? proof)
    {
        proof = null;
        if (!_settings.CanUseMoMo || json.ValueKind != JsonValueKind.Object) return false;
        string Str(string name) => json.TryGetProperty(name, out var v) ?
            v.ValueKind switch { JsonValueKind.String => v.GetString() ?? "", JsonValueKind.Number => v.GetRawText(), _ => "" } : "";
        var partner = Str("partnerCode");
        var reference = Str("orderId");
        var requestId = Str("requestId");
        var amount = Str("amount");
        var extra = Str("extraData");
        var message = Str("message");
        var info = Str("orderInfo");
        var type = Str("orderType");
        var payType = Str("payType");
        var responseTime = Str("responseTime");
        var resultCode = Str("resultCode");
        var transId = Str("transId");
        var signature = Str("signature");
        if (partner != _settings.MoMo.PartnerCode || reference.Length is < 10 or > 100 ||
            requestId != reference || !long.TryParse(amount, NumberStyles.None, CultureInfo.InvariantCulture, out var amountVnd) ||
            amountVnd <= 0 || !int.TryParse(resultCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) ||
            signature.Length != 64) return false;
        var raw = $"accessKey={_settings.MoMo.AccessKey}&amount={amount}&extraData={extra}&message={message}&orderId={reference}&orderInfo={info}&orderType={type}&partnerCode={partner}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
        var expected = Hmac(_settings.MoMo.SecretKey, raw);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signature.ToLowerInvariant()))) return false;
        proof = new PaymentProof("MOMO", reference, amountVnd, code is 9000 or 7000 or 7002 ? (bool?)null : code == 0, transId);
        return true;
    }
}
