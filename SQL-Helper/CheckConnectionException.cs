namespace BiemannT.SQLHelper
{
    /// <summary>
    /// Represents an exception that is thrown when a connection check operation fails due to a specific error
    /// condition.
    /// </summary>
    /// <remarks>Use this exception to capture and handle errors encountered during connection validation,
    /// such as network issues, authentication failures, or database errors. The exception provides additional context
    /// through the <see cref="ExceptionType"/> and <see cref="SqlErrorNumber"/> properties, enabling more granular
    /// error handling and diagnostics.</remarks>
    public class CheckConnectionException : Exception
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckConnectionException"/> class with a specified error message and
        /// exception type.
        /// </summary>
        /// <param name="message">The error message that describes the reason for the exception.</param>
        /// <param name="exceptionType">The type of the connection check exception, indicating the specific error condition encountered.</param>
        public CheckConnectionException(string message, CheckConnectionExceptionType exceptionType) : base(message)
        {
            ExceptionType = exceptionType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckConnectionException"/> class with a specified error message, exception
        /// type, and SQL error number.
        /// </summary>
        /// <param name="message">The error message that describes the reason for the exception.</param>
        /// <param name="exceptionType">The type of connection check exception that occurred.</param>
        /// <param name="sqlErrorNumber">The SQL Server error number associated with the exception.</param>
        public CheckConnectionException(string message, CheckConnectionExceptionType exceptionType, int sqlErrorNumber) : base(message)
        {
            ExceptionType = exceptionType;
            SqlErrorNumber = sqlErrorNumber;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckConnectionException"/> class with a specified error message, exception
        /// type, SQL error number, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="exceptionType">The type of connection check exception that occurred.</param>
        /// <param name="sqlErrorNumber">The SQL error number associated with the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CheckConnectionException(string message, CheckConnectionExceptionType exceptionType, int sqlErrorNumber, Exception innerException) : base(message, innerException)
        {
            ExceptionType = exceptionType;
            SqlErrorNumber = sqlErrorNumber;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckConnectionException"/> class with a specified error message and a
        /// reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is
        /// specified.</param>
        public CheckConnectionException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Gets the type of exception that occurred during the connection check operation.
        /// </summary>
        /// <remarks>Use this property to determine the specific reason for a connection check failure.
        /// The value can be used to implement custom error handling or logging based on the exception type.</remarks>
        public CheckConnectionExceptionType ExceptionType { get; init; } = CheckConnectionExceptionType.UnhandledException;

        /// <summary>
        /// Gets the SQL Server error number associated with the operation, if available.
        /// </summary>
        /// <remarks>This property is typically set when an error occurs during a database operation
        /// involving SQL Server. The value corresponds to the error number returned by SQL Server, or -1 if no error
        /// number is available.</remarks>
        public int SqlErrorNumber { get; init; } = -1;
    }
}
