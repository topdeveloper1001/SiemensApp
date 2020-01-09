// Since DbUp does not support using a managed identity to connect Azure SQL Database, we copied
// the code from DbUp and modified a bit to support this new secure approach.
// https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-connect-msi
// https://github.com/aspnet/EntityFrameworkCore/issues/11928
// https://github.com/DbUp/DbUp/blob/4.2.0/src/dbup-sqlserver/SqlServerExtensions.cs

// Due to an unknown reason, the following method must be modified in order to let Coverlet collect code coverage successfully. 
// public static void AzureSqlDatabase(
//     this SupportedDatabasesForEnsureDatabase supported, 
//     string connectionString, 
//     IUpgradeLog logger, 
//     int timeout = -1, 
//     AzureDatabaseEdition azureDatabaseEdition = AzureDatabaseEdition.None,   <<===== This parameter is removed
//     string collation = null)

using System;
using System.Data;
using System.Data.SqlClient;
using DbUp;
using DbUp.Builder;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.SqlServer;

namespace SiemensApp.Database.DbUp
{
/// <summary>
/// Configuration extension methods for SQL Server.
/// </summary>
// NOTE: DO NOT MOVE THIS TO A NAMESPACE
// Since the class just contains extension methods, we leave it in the global:: namespace so that it is always available
// ReSharper disable CheckNamespace
public static class AzureSqlServerExtensions
// ReSharper restore CheckNamespace
{
    /// <summary>
    /// Creates an upgrader for SQL Server databases.
    /// </summary>
    /// <param name="supported">Fluent helper type.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>
    /// A builder for a database upgrader designed for SQL Server databases.
    /// </returns>
    public static UpgradeEngineBuilder AzureSqlDatabase(this SupportedDatabases supported, string connectionString)
    {
        return AzureSqlDatabase(supported, connectionString, null);
    }

    /// <summary>
    /// Creates an upgrader for SQL Server databases.
    /// </summary>
    /// <param name="supported">Fluent helper type.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="schema">The SQL schema name to use. Defaults to 'dbo'.</param>
    /// <returns>
    /// A builder for a database upgrader designed for SQL Server databases.
    /// </returns>
    public static UpgradeEngineBuilder AzureSqlDatabase(this SupportedDatabases supported, string connectionString, string schema)
    {
        return AzureSqlDatabase(new AzureSqlConnectionManager(connectionString), schema);
    }

    /// <summary>
    /// Creates an upgrader for SQL Server databases.
    /// </summary>
    /// <param name="connectionManager">The <see cref="IConnectionManager"/> to be used during a database
    /// upgrade. See <see cref="AzureSqlConnectionManager"/> for an example implementation</param>
    /// <param name="schema">The SQL schema name to use. Defaults to 'dbo'.</param>
    /// <returns>
    /// A builder for a database upgrader designed for SQL Server databases.
    /// </returns>
    private static UpgradeEngineBuilder AzureSqlDatabase(IConnectionManager connectionManager, string schema)
    {
        var builder = new UpgradeEngineBuilder();
        builder.Configure(c => c.ConnectionManager = connectionManager);
        builder.Configure(c => c.ScriptExecutor = new SqlScriptExecutor(() => c.ConnectionManager, () => c.Log, schema, () => c.VariablesEnabled, c.ScriptPreprocessors, () => c.Journal));
        builder.Configure(c => c.Journal = new SqlTableJournal(() => c.ConnectionManager, () => c.Log, schema, "SchemaVersions"));
        return builder;
    }

    /// <summary>
    /// Ensures that the database specified in the connection string exists.
    /// </summary>
    /// <param name="supported">Fluent helper type.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="logger">The <see cref="DbUp.Engine.Output.IUpgradeLog"/> used to record actions.</param>
    /// <param name="timeout">Use this to set the command time out for creating a database in case you're encountering a time out in this operation.</param>
    /// <param name="azureDatabaseEdition">Use to indicate that the SQL server database is in Azure</param>
    /// <param name="collation">The collation name to set during database creation</param>
    /// <returns></returns>
    public static void AzureSqlDatabase(
        this SupportedDatabasesForEnsureDatabase supported, 
        string connectionString, 
        IUpgradeLog logger, 
        int timeout = -1, 
        string collation = null)
    {
        string databaseName;
        string masterConnectionString;
        GetMasterConnectionStringBuilder(connectionString, logger, out masterConnectionString, out databaseName);

        using (var connection = new SqlConnection(masterConnectionString))
        {
            if (connection.UseManagedIdentity())
            {
                logger.WriteInformation("Use ManagedIndentity for the connection: {0}", masterConnectionString);
            }
            connection.Open();

            var sqlCommandText = string.Format
                (
                    @"SELECT TOP 1 case WHEN dbid IS NOT NULL THEN 1 ELSE 0 end FROM sys.sysdatabases WHERE name = '{0}';",
                    databaseName
                );


            // check to see if the database already exists..
            using (var command = new SqlCommand(sqlCommandText, connection)
            {
                CommandType = CommandType.Text
            })

            {
                var results = (int?) command.ExecuteScalar();

                // if the database exists, we're done here...
                if (results.HasValue && results.Value == 1)
                {
                    return;
                }
            }

            string collationString = string.IsNullOrEmpty(collation) ? "" : string.Format(@" COLLATE {0}", collation);
            sqlCommandText = string.Format
                    (
                        @"create database [{0}]{1};",
                        databaseName,
                        collationString
                    );

        // Create the database...
            using (var command = new SqlCommand(sqlCommandText, connection)
            {
                CommandType = CommandType.Text
            })
            {
                if (timeout >= 0)
                {
                    command.CommandTimeout = timeout;
                }

                command.ExecuteNonQuery();

            }

            logger.WriteInformation(@"Created database {0}", databaseName);
        }
    }

    private static void GetMasterConnectionStringBuilder(string connectionString, IUpgradeLog logger, out string masterConnectionString, out string databaseName)
    {
        if (string.IsNullOrEmpty(connectionString) || connectionString.Trim() == string.Empty)
            throw new ArgumentNullException("connectionString");

        if (logger == null)
            throw new ArgumentNullException("logger");

        var masterConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        databaseName = masterConnectionStringBuilder.InitialCatalog;

        if (string.IsNullOrEmpty(databaseName) || databaseName.Trim() == string.Empty)
            throw new InvalidOperationException("The connection string does not specify a database name.");

        masterConnectionStringBuilder.InitialCatalog = "master";
        var logMasterConnectionStringBuilder = new SqlConnectionStringBuilder(masterConnectionStringBuilder.ConnectionString)
        {
            Password = string.Empty.PadRight(masterConnectionStringBuilder.Password.Length, '*')
        };

        logger.WriteInformation("Master ConnectionString => {0}", logMasterConnectionStringBuilder.ConnectionString);
        masterConnectionString = masterConnectionStringBuilder.ConnectionString;
    }
}
}