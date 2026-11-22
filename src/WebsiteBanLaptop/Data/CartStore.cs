using System.Text.Json;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Data;
public static class CartStore
{
    private const string Key = "GioHang";
    public static List<CartItem> Get(ISession session)
    {
        var text = session.GetString(Key);
        if (string.IsNullOrEmpty(text)) return new List<CartItem>();
        return JsonSerializer.Deserialize<List<CartItem>>(text) ?? new List<CartItem>();
    }
    public static void Save(ISession session, List<CartItem> cart)
    {
        session.SetString(Key, JsonSerializer.Serialize(cart));
    }
}
