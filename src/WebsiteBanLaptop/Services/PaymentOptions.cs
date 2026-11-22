namespace WebsiteBanLaptop.Services;

public sealed class PaymentOptions
{
    public const string SectionName = "PaymentGateways";
    public string PublicBaseUrl { get; set; } = "";
    public VnPayOptions VnPay { get; set; } = new();
    public MoMoOptions MoMo { get; set; } = new();

    private static bool TrustedUrl(string value, string hostname, string pathname)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var url) &&
            url.Scheme == Uri.UriSchemeHttps &&
            url.Host.Equals(hostname, StringComparison.OrdinalIgnoreCase) &&
            url.AbsolutePath.Equals(pathname, StringComparison.Ordinal) &&
            url.IsDefaultPort && string.IsNullOrEmpty(url.Query) && string.IsNullOrEmpty(url.Fragment);
    }

    public bool HasPublicHttpsAddress() => Uri.TryCreate(PublicBaseUrl, UriKind.Absolute,
        out var address) && address.Scheme == Uri.UriSchemeHttps &&
        !address.IsLoopback && string.IsNullOrEmpty(address.Query) &&
        string.IsNullOrEmpty(address.Fragment) &&
        address.AbsolutePath == "/" && address.IsDefaultPort;

    public bool CanUseVnPay => HasPublicHttpsAddress() && VnPay.Enabled &&
        !string.IsNullOrWhiteSpace(VnPay.TmnCode) && !string.IsNullOrWhiteSpace(VnPay.HashSecret) &&
        TrustedUrl(VnPay.PaymentUrl, "pay.vnpay.vn", "/vpcpay.html");

    public bool CanUseMoMo => HasPublicHttpsAddress() && MoMo.Enabled &&
        !string.IsNullOrWhiteSpace(MoMo.PartnerCode) && !string.IsNullOrWhiteSpace(MoMo.AccessKey) &&
        !string.IsNullOrWhiteSpace(MoMo.SecretKey) &&
        TrustedUrl(MoMo.CreateUrl, "payment.momo.vn", "/v2/gateway/api/create");
}

public sealed class VnPayOptions
{
    public bool Enabled { get; set; }
    public string TmnCode { get; set; } = "";
    public string HashSecret { get; set; } = "";
    public string PaymentUrl { get; set; } = "https://pay.vnpay.vn/vpcpay.html";
}

public sealed class MoMoOptions
{
    public bool Enabled { get; set; }
    public string PartnerCode { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string CreateUrl { get; set; } = "https://payment.momo.vn/v2/gateway/api/create";
}
