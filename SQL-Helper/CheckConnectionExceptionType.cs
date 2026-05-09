namespace BiemannT.SQLHelper
{
    /// <summary>
    /// Specifies the types of exceptions that can occur when checking a database connection.
    /// </summary>
    /// <remarks>Use this enumeration to identify the specific reason for a connection check failure, such as
    /// missing credentials, expired password, or network issues. The values can be used to provide detailed error
    /// handling or user feedback based on the exception type.</remarks>
    public enum CheckConnectionExceptionType
    {
        /// <summary>
        /// Indicates that an exception was not handled by the application or its error handling mechanisms.
        /// </summary>
        UnhandledException = 0,

        /// <summary>
        /// Indicates that no login name is set for the database connection.
        /// </summary>
        NoLoginName = 1,

        /// <summary>
        /// Indicates that no password is set for the database connection.
        /// </summary>
        NoPassword = 2,

        /// <summary>
        /// Indicates that authentication failed because the user's password has expired.
        /// </summary>
        PasswordExpired = 3,

        /// <summary>
        /// Indicates that a password change is required for the user or account.
        /// </summary>
        PasswordChangeRequired = 4,

        /// <summary>
        /// Indicates that no server IP address is available or has been specified.
        /// </summary>
        NoServerIP = 5,

        /// <summary>
        /// Indicates that the account is disabled in the SQL Server.
        /// </summary>
        SqlAccountDisabled = 6,

        /// <summary>
        /// Indicates that the login attempt failed due to an incorrect username or password.
        /// </summary>
        WrongLoginNameOrPassword = 7,

        /// <summary>
        /// Indicates that the connection attempt to the database server timed out, due to network issues or server unavailability.
        /// </summary>
        ConnectionTimeout = 8,

        /// <summary>
        /// Indicates that access to the database is not permitted.
        /// </summary>
        NoDatabaseAccess = 9,

        /// <summary>
        /// The request was canceled before it could be completed.
        /// </summary>
        RequestCancelled = 10,
    }
}
