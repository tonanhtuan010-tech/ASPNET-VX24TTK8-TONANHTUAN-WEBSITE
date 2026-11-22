using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Data;

public static class CatalogData
{
    public static async Task RestoreIfEmptyAsync(AppDbContext db)
    {
        if (await db.Products.AnyAsync()) return;

        await using var transaction = await db.Database.BeginTransactionAsync();
        if (await db.Products.AnyAsync()) return;

        var brands = new[] { "ASUS", "Lenovo", "Dell", "HP", "Acer", "Apple", "MSI" };
        var current = await db.Categories.AsNoTracking()
            .Select(category => category.CategoryName).ToListAsync();
        var existing = new HashSet<string>(current, StringComparer.OrdinalIgnoreCase);
        foreach (var brand in brands)
        {
            if (existing.Add(brand))
                db.Categories.Add(new Category { CategoryName = brand });
        }
        await db.SaveChangesAsync();

        var cats = (await db.Categories.AsNoTracking().ToListAsync())
            .ToDictionary(category => category.CategoryName, category => category.CategoryID,
                StringComparer.OrdinalIgnoreCase);
        db.Products.AddRange(new[]
        {
            P("ASUS Vivobook 15 X1504VA Core 3 100U",17690000,"ASUS","Intel Core 3 100U","8GB","512GB SSD","Intel Graphics","15.6 inch FHD",16,1,cats),
            P("ASUS Vivobook 15 X1504VA Core 5 120U",19290000,"ASUS","Intel Core 5 120U","16GB","512GB SSD","Intel Graphics","15.6 inch FHD",18,2,cats),
            P("ASUS Vivobook 15 X1504VA Core 7 150U",23090000,"ASUS","Intel Core 7 150U","16GB","512GB SSD","Intel Graphics","15.6 inch FHD",11,3,cats),
            P("ASUS TUF Gaming FA506NCQ",24990000,"ASUS","AMD Ryzen 7 170","16GB","512GB SSD","GeForce RTX 3050 4GB","15.6 inch FHD 144Hz",9,4,cats),
            P("Lenovo IdeaPad Slim 3 15IWC11",24190000,"Lenovo","Intel Core 5 320","16GB","512GB SSD","Intel Graphics","15.6 inch",14,5,cats),
            P("Lenovo LOQ 15IRX10 83JE01AGVN",37590000,"Lenovo","Intel Core i7-13645HX","16GB","512GB SSD","GeForce RTX 5050 8GB","15.6 inch FHD 144Hz",7,6,cats),
            P("Dell 15 DC15250",22990000,"Dell","Intel Core i5-1334U","16GB","512GB SSD","Intel UHD","15.6 inch FHD",12,7,cats),
            P("HP 245 G10",19290000,"HP","AMD Ryzen 5 7530U","8GB","512GB SSD","AMD Radeon","14 inch FHD",10,8,cats),
            P("Acer Aspire Go 15 AG15-72P",19490000,"Acer","Intel Core 5 120U","16GB","512GB SSD","Intel Graphics","15.6 inch FHD",13,9,cats),
            P("MacBook Air 13 M4",25290000,"Apple","Apple M4","16GB","256GB SSD","Apple GPU 8 nhân","13.6 inch Liquid Retina",6,10,cats),
            P("MSI Modern 15 F13MG",18190000,"MSI","Intel Core i5-1334U","16GB","512GB SSD","Intel UHD","15.6 inch FHD",8,11,cats),
            P("ASUS Vivobook Go 15 E1504FA",19390000,"ASUS","AMD Ryzen 5 7520U","8GB","512GB SSD","AMD Radeon","15.6 inch FHD",16,12,cats),
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    private static string ProductImage(int image)
    {
        string[] urls =
        {
            "https://www.tnc.com.vn/uploads/product/sp2026/laptop-asus-vivobook-15-x1504va-bq265w.webp",
            "https://www.tnc.com.vn/uploads/product/sp2026/laptop-asus-vivobook-15-x1504va-bq265w.webp",
            "https://www.tnc.com.vn/uploads/product/sp2026/laptop-asus-vivobook-15-x1504va-bq265w.webp",
            "https://estilospe.vtexassets.com/arquivos/ids/3538932-800-800?aspect=true&height=800&v=638852790544170000&width=800",
            "https://psrefstuff.lenovo.com/syspool/Sys/Image/IdeaPad/IdeaPad_Slim_3_15IWC11/Compressedimage/IdeaPad_Slim_3_15IWC11_CT2_09.png",
            "https://dwecxxryy5p59.cloudfront.net/catalogs/3/f/2/8/3f28c9509414f9db294c30c50ca8f756ea50c7d9_imgi_165_LOQ_15IRX10_CT1_05.png",
            "https://cdn.ankhang.vn/media/product/31315_laptop_dell_15_dc15250_i5u165w11slu_27__1_.jpg",
            "https://jp.ext.hp.com/content/dam/jp-ext-hp-com/jp/ja/ec/notebooks/business/hp_245_g10/images/design_05.jpg",
            "https://www.firstshop.co.za/cdn/shop/files/nx-jrrea-009-windows-laptops-1232345579.jpg?v=1776156057&width=1000",
            "https://www.pbtech.co.nz/imgprod/N/B/NBKAPP341325611__1.jpg",
            "https://de-store.msi.com/cdn/shop/files/Modern_15_F13M_Platinum_Gray_photo_02.png?v=1756913184&width=1214",
            "https://thumb.pccomponentes.com/w-530-530/articles/1087/10878055/3598-asus-vivobook-go-15-e1504fa-bq2447-amd-ryzen-5-7520u-16gb-512gb-ssd-156-mejor-precio.jpg"
        };
        return image >= 1 && image <= urls.Length
            ? urls[image - 1]
            : "/images/laptop-default.svg";
    }
    private static Product P(string name, decimal price, string category, string cpu,
        string ram, string storage, string graphics, string screen, int stock, int image,
        Dictionary<string,int> cats)
    {
        return new Product
        {
            Name = name, Price = price, CategoryID = cats[category], Cpu = cpu, Ram = ram,
            Storage = storage, Graphics = graphics, Screen = screen, Stock = stock,
            ImageUrl = ProductImage(image),
            Description = $"{name}: {cpu}, RAM {ram}, ổ cứng {storage}, màn hình {screen}."
        };
    }
}
