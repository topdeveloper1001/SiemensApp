// Since DbUp does not support using a managed identity to connect Azure SQL Database, we copied
// the code from DbUp and modified a bit to support this new secure approach.
// https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-connect-msi
// https://github.com/aspnet/EntityFrameworkCore/issues/11928
// https://github.com/DbUp/DbUp/blob/4.2.0/src/dbup-sqlserver/SqlConnectionManager.cs

using System.Collections.Generic;
using System.Data.SqlClient;
using DbUp.Engine.Transactions;
using DbUp.Support;

namespace SiemensApp.Database.DbUp
{
    /// <summary>
    /// Manages Sql Database Connections
    /// </summary>
    public class AzureSqlConnectionManager : DatabaseConnectionManager
    {
        /// <summary>
        /// Manages Sql Database Connections
        /// </summary>
        /// <param name="connectionString"></param>
        public AzureSqlConnectionManager(string connectionString)
            : base(new DelegateConnectionFactory((log, dbManager) =>
            {
                var conn = new SqlConnection(connectionString);
                if (conn.UseManagedIdentity())
                {
                    log.WriteInformation("Use ManagedIndentity for the connection: {0}", connectionString);
                }

                if (dbManager.IsScriptOutputLogged)
                    conn.InfoMessage += (sender, e) => log.WriteInformation("{0}\r\n", e.Message);

                return conn;
            }))
        {
        }

        public override IEnumerable<string> SplitScriptIntoCommands(string scriptContents)
        {
            var commandSplitter = new SqlCommandSplitter();
            var scriptStatements = commandSplitter.SplitScriptIntoCommands(scriptContents);
            return scriptStatements;
        }
    }
}