using Microsoft.Data.SqlClient;

namespace BiemannT.SQLHelper
{
    /// <summary>
    /// Encapsulates the information required to establish a connection to a SQL Server database, including the
    /// connection string and authentication credentials.
    /// </summary>
    /// <param name="connectionString">The connection string used to configure and establish the SQL Server database connection. Must be a valid
    /// connection string; cannot be null.</param>
    /// <param name="credential">The SQL Server credential used for authentication when connecting to the database. Cannot be null.</param>
    public class SqlConnInfo(string connectionString, SqlCredential credential)
    {
        /// <summary>
        /// Gets the connection string used to establish a connection to the SQL database.
        /// </summary>
        public string ConnectionString { get; init; } = connectionString;

        /// <summary>
        /// Gets the SQL Server credential used for authentication when establishing a connection.
        /// </summary>
        public SqlCredential Credential { get; init; } = credential;
    }
}
