using Microsoft.EntityFrameworkCore;

namespace TechStoreWeb.Data
{
    public static class OrderReceiverInfoInitializer
    {
        public static void EnsureCreated(AppDbContext context)
        {
            context.Database.ExecuteSqlRaw("""
                IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'[dbo].[Orders]', N'ReceiverName') IS NULL
                        ALTER TABLE [dbo].[Orders] ADD [ReceiverName] [nvarchar](max) NULL;

                    IF COL_LENGTH(N'[dbo].[Orders]', N'ReceiverPhone') IS NULL
                        ALTER TABLE [dbo].[Orders] ADD [ReceiverPhone] [nvarchar](max) NULL;

                    IF COL_LENGTH(N'[dbo].[Orders]', N'ReceiverEmail') IS NULL
                        ALTER TABLE [dbo].[Orders] ADD [ReceiverEmail] [nvarchar](max) NULL;

                    IF COL_LENGTH(N'[dbo].[Orders]', N'Note') IS NULL
                        ALTER TABLE [dbo].[Orders] ADD [Note] [nvarchar](max) NULL;

                    IF COL_LENGTH(N'[dbo].[Orders]', N'ShippingMethod') IS NULL
                        ALTER TABLE [dbo].[Orders] ADD [ShippingMethod] [nvarchar](max) NULL;

                    IF COL_LENGTH(N'[dbo].[Orders]', N'ShippingFee') IS NULL
                        ALTER TABLE [dbo].[Orders] ADD [ShippingFee] [decimal](18,2) NOT NULL
                            CONSTRAINT [DF_Orders_ShippingFee] DEFAULT (0);

                    IF COL_LENGTH(N'[dbo].[Orders]', N'InsuranceFee') IS NULL
                        ALTER TABLE [dbo].[Orders] ADD [InsuranceFee] [decimal](18,2) NOT NULL
                            CONSTRAINT [DF_Orders_InsuranceFee] DEFAULT (0);
                END
                """);
        }
    }
}
