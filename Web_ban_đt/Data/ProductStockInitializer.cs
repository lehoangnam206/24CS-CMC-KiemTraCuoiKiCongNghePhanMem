using Microsoft.EntityFrameworkCore;

namespace TechStoreWeb.Data
{
    public static class ProductStockInitializer
    {
        public static void EnsureSynced(AppDbContext context)
        {
            context.Database.ExecuteSqlRaw("""
                IF OBJECT_ID(N'[dbo].[ProductVariants]', N'U') IS NOT NULL
                   AND OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
                BEGIN
                    UPDATE p
                        SET p.[Stock] = v.[TotalStock]
                    FROM [dbo].[Products] p
                    INNER JOIN (
                        SELECT [ProductId], SUM([Stock]) AS [TotalStock]
                        FROM [dbo].[ProductVariants]
                        GROUP BY [ProductId]
                    ) v ON v.[ProductId] = p.[ProductId]
                    WHERE p.[Stock] <> v.[TotalStock];
                END
                """);
        }
    }
}
