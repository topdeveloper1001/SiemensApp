using Microsoft.Azure.Services.AppAuthentication;

namespace System.Data.SqlClient
{
    public static class SqlConnectionExtensionsForAzure
    {
        private static readonly string[] ConflictKeywords = new string[] { "Password=", "pwd=" };

        public static bool UseManagedIdentity(this SqlConnection sqlConnection)
        {
            var connectionString = sqlConnection.ConnectionString;
            foreach (var keyword in ConflictKeywords)
            {
                if (connectionString.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            var tokenProvider = new AzureServiceTokenProvider();
            sqlConnection.AccessToken = tokenProvider.GetAccessTokenAsync("https://database.windows.net/").Result;
            return true;
        }
    }
}