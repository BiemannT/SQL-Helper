# SQL-Helper
A simple helper for executing SQL queries and commands in C#.
It provides methods for executing non-query commands, scalar queries, and reader queries, making it easier to interact with a SQL database.

The library is designed to establish a connection to a SQL database using
the SQL authentication method. The main intention is to split the handling
of the connection and the execution of SQL commands. One program class will setup
the connection and pass it to another class that will execute the SQL commands.
Once the connection is prepared, the instance can be used to execute several SQL commands.

## Usage
The main class of this library is `SetupSqlConnection`. It contains properties to prepare
the connection and methods to execute SQL commands.

### Create connection
Create an instance of `SetupSqlConnection` and set the necessary properties to establish a connection to the SQL database.
```csharp
SetupSqlConnection sqlConnection = new() {
	ServerLoginName = "your_login_name", // Required. The login name for SQL authentication.
	ServerLoginPassword = your_login_password, // Required. Must be of type `SecureString`. Make sure to call `MakeReadOnly()` on the `SecureString` instance before assigning it.
	ServerPort = 1433, // Optional. Default SQL Server port
	Database = "your_database_name", // Required. The name of the database to connect to.
	ServerInstance = "your_server_instance" // Optional, if you are connecting to a named instance. Otherwise, leave it empty to connect to the default instance.
};
```

The server IP-address can be assigned to the property `ServerIP` only by using the methods `ParseServerIP()` or `TryParseServerIP()`.

Using the `ParseServerIP(string ServerNameOrIP, [bool CheckDNS = false])` method:\
You can pass either a server name, full-qualified domain name (FQDN), or an IPv4 address as a string.
If you pass a server name or FQDN, the parameter `CheckDNS` must be set to `true` to resolve the IPv4 address.
In this case a registered DNS server is required to resolve the server name or FQDN to an IP address.
If `CheckDNS` is set to `false`, the method will attempt to parse the input as an IP address directly, which will fail if the input is not in a valid IP address format.
```csharp
try
{
	sqlConnection.ParseServerIP("your_server_name_or_ip", true);

	// Alternative without DNS check
	// sqlConnection.ParseServerIP("your_server_ip", false);
}
catch (Exception ex)
{
	Console.WriteLine($"Error parsing server IP: {ex.Message}");
}
```

Using the `TryParseServerIP(string ServerNameOrIP, [bool CheckDNS = false])` method:\
This method works similarly to `ParseServerIP`, but instead of throwing an exception on failure, it returns a boolean indicating success or failure.
```csharp
bool success = sqlConnection.TryParseServerIP("your_server_name_or_ip", true);

// Alternative without DNS check
// bool success = sqlConnection.TryParseServerIP("your_server_ip", false);

if (!success)
{
	Console.WriteLine("Failed to parse server IP.");
}
```

After preparing the connection properties, you can call the `CheckConnection()` or `CheckConnectionAsync()` method
to verify that the connection to the SQL database can be established successfully.
This method will be also called internally before executing any SQL command to ensure that the connection is valid.
The state of the last check is stored in the property `IsConnectionChecked`,
which can be used to avoid redundant checks if the connection state is already known.
If any connection fails, the property `IsConnectionChecked` will be set to `false`.
```csharp
try
{
	sqlConnection.CheckConnection();

	if (sqlConnection.IsConnectionChecked)
	{
		Console.WriteLine("Connection is valid.");
	}
}
catch (CheckConnectionException ex)
{
	Console.WriteLine($"Error checking connection: {ex.Message}");
}
catch (Exception ex)
{
	Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

In case the connection check fails, a `CheckConnectionException` will be thrown,
which provides details about the failure.
The property `ExceptionType` is of type `CheckConnectionExceptionType` and represents typical reasons for connection failures.

### Execute SQL commands
Once the connection is established and verified, you can use the following methods to execute SQL commands:
+ `ExecNonQuery(SqlCommand command)`: Executes a SQL command and return the number of rows affected.
+ `ExecScalar(SqlCommand command)`: Executes a SQL command and return the first column of the first row in the result set.
+ `ExecReader(SqlCommand command)`: Executes a SQL command and return a `SqlDataReader` to read the results.

There are also asynchronous versions of these methods, with an additional optional parameter for cancellation:
+ `ExecNonQueryAsync(SqlCommand command, CancellationToken? cancellation = null)`
+ `ExecScalarAsync(SqlCommand command, CancellationToken? cancellation = null)`
+ `ExecReaderAsync(SqlCommand command, CancellationToken? cancellation = null)`

In case of an error during the execution of these methods, 
a `CheckConnectionException` will be thrown if the error arises from a connection issue,
or a `SqlException` will be thrown for other SQL-related errors.

The main intention to use these methods is to create a class with methods
that accept the prepared `SetupSqlConnection` instance
and execute the SQL commands using the connection provided by the instance.

Example:
```csharp
public void ExecuteSomething(SetupSqlConnection sqlConnection)
{
	try
	{
		// Prepare your SQL command
		using SqlCommand command new()
		{
			CommandType = CommandType.StoredProcedure,
			CommandText = "[dbo].[YourStoredProcedureName]",
			CommandTimeout = 30 // Optional, default is 30 seconds
		};

		// Execute the command using the provided sqlConnection instance
		int rowsAffected = sqlConnection.ExecNonQuery(command);
	}
	catch (CheckConnectionException ex)
	{
		Console.WriteLine($"Connection error: {ex.Message}");
	}
	catch (SqlException ex)
	{
		Console.WriteLine($"SQL error: {ex.Message}");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Unexpected error: {ex.Message}");
	}
}
```