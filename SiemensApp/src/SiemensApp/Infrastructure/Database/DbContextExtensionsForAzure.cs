using System.Data.SqlClient;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextExtensionsForAzure
    {
        public static bool UseManagedIdentity(this DbContext dbContext)
        {
            if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.SqlServer")
            {
                return false;
            }
            var conn = (SqlConnection)dbContext.Database.GetDbConnection();
            return conn.UseManagedIdentity();
        }
    }
}