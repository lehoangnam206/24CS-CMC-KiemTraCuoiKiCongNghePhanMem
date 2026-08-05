using Microsoft.EntityFrameworkCore;

namespace TechStoreWeb.Data
{
    public static class UserPermissionsInitializer
    {
        public static void EnsureCreated(AppDbContext context)
        {
            context.Database.ExecuteSqlRaw("""
                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[Users]', N'Permissions') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [Permissions] [nvarchar](max) NULL;
                END
                """);
        }
    }
}
