using Microsoft.EntityFrameworkCore;

namespace TechStoreWeb.Data
{
    public static class ProductCostPriceInitializer
    {
        public static void EnsureCreated(AppDbContext context)
        {
            context.Database.ExecuteSqlRaw("""
                IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[Products]', N'CostPrice') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Products] ADD [CostPrice] [decimal](18,2) NULL;
                END
                """);
        }
    }
}
