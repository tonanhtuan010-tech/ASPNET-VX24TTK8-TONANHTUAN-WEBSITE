using Microsoft.EntityFrameworkCore;

namespace WebsiteBanLaptop.Data;

public static class StageSchema
{
    public static Task EnsureAsync(AppDbContext db) => db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Categories] (
                    [CategoryID] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Categories] PRIMARY KEY,
                    [CategoryName] nvarchar(60) NOT NULL
                );
                CREATE UNIQUE INDEX [IX_Categories_CategoryName]
                    ON [dbo].[Categories] ([CategoryName]);
            END;
            IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Products] (
                    [ProductID] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Products] PRIMARY KEY,
                    [Name] nvarchar(200) NOT NULL,
                    [Price] decimal(18,0) NOT NULL,
                    [CategoryID] int NOT NULL,
                    [Description] nvarchar(3000) NOT NULL,
                    [ImageUrl] nvarchar(500) NULL,
                    [Stock] int NOT NULL,
                    [Cpu] nvarchar(150) NOT NULL,
                    [Ram] nvarchar(80) NOT NULL,
                    [Storage] nvarchar(80) NOT NULL,
                    [Graphics] nvarchar(120) NOT NULL,
                    [Screen] nvarchar(130) NOT NULL,
                    CONSTRAINT [FK_Products_Categories_CategoryID]
                        FOREIGN KEY ([CategoryID]) REFERENCES [dbo].[Categories] ([CategoryID])
                        ON DELETE NO ACTION
                );
                CREATE INDEX [IX_Products_CategoryID]
                    ON [dbo].[Products] ([CategoryID]);
            END;
            """);
}
