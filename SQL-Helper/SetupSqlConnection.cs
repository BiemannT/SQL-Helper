using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security;

namespace BiemannT.SQLHelper
{
    /// <summary>
    /// Provides configuration and management of SQL Server connection settings, authentication, and connection
    /// operations for establishing and executing commands against a SQL Server database.
    /// </summary>
    /// <remarks>This class encapsulates all necessary parameters for connecting to a SQL Server instance,
    /// including server address resolution, authentication credentials, and connection timeouts. It
    /// offers methods for validating connection settings, building connection strings, and executing SQL commands
    /// synchronously. The class implements IDisposable to ensure proper release of resources, such as secure
    /// credentials. This class is designed to be thread-safe, allowing concurrent access to its properties and methods
    /// from multiple threads without risking data corruption or inconsistent states.
    /// The class also implements INotifyPropertyChanged to support data binding scenarios,
    /// allowing UI components or other observers to react to changes in connection settings or status.
    /// </remarks>
    public class SetupSqlConnection : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the SetupSqlConnection class with default connection settings.
        /// </summary>
        /// <remarks>This constructor sets default values for all connection parameters, including the SQL
        /// Server port, retry policy and timeout. These
        /// defaults can be modified after construction as needed before establishing a connection.</remarks>
        public SetupSqlConnection() { }

#if NET9_0_OR_GREATER
        /// <summary>
        /// Provides a lock object used to synchronize access to class information.
        /// </summary>
        /// <remarks>This lock ensures thread-safe operations when accessing or modifying class-level
        /// metadata. It should be acquired before performing any actions that require exclusive access to shared class
        /// information.</remarks>
        private readonly System.Threading.Lock _classInfoLock = new();

        /// <summary>
        /// Provides a lock object used to synchronize access to connection information of login credentials.
        /// </summary>
        /// <remarks>
        /// This lock ensures thread-safe operations when accessing or modifying connection-related
        /// metadata, particularly for login credentials. It should be acquired before performing any actions that
        /// require exclusive access to shared connection information.
        /// </remarks>
        private readonly System.Threading.Lock _connectionInfoLock = new();
#else
        /// <summary>
        /// Provides a lock object used to synchronize access to class information.
        /// </summary>
        /// <remarks>This lock ensures thread-safe operations when accessing or modifying class-level
        /// metadata. It should be acquired before performing any actions that require exclusive access to shared class
        /// information.</remarks>
        private readonly object _classInfoLock = new();

        /// <summary>
        /// Provides a lock object used to synchronize access to connection information of login credentials.
        /// </summary>
        /// <remarks>
        /// This lock ensures thread-safe operations when accessing or modifying connection-related
        /// metadata, particularly for login credentials. It should be acquired before performing any actions that
        /// require exclusive access to shared connection information.
        /// </remarks>
        private readonly object _connectionInfoLock = new();
#endif

        #region Property ServerIP

        /// <summary>
        /// Stores the value of the corresponding property <see cref="ServerIP"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_classInfoLock"/> before accessing this variable.
        /// </remarks>
        private IPHostEntry _serverIP = new();

        /// <summary>
        /// Gets or sets the DNS host information for the SQL Server connection.
        /// </summary>
        /// <remarks>
        /// On changing this property, the connection check status is reset to <see langword="false"/> and the cached connection string is cleared.
        /// This property is thread-safe.
        /// </remarks>
        public IPHostEntry ServerIP
        {
            get
            {
                lock (this._classInfoLock)
                {
                    return this._serverIP;
                }
            }

            protected set
            {
                lock (this._classInfoLock)
                {
                    // Wurde der Hostname geändert?
                    if (value.HostName != this._serverIP.HostName)
                    {
                        this.IsConnectionChecked = false;
                        this.ConnectionInfo = null;
                        this._serverIP = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Parses the specified server name or IP address and resolves it to an IP address, optionally performing a DNS lookup.
        /// </summary>
        /// <remarks>When CheckDNS is <see langword="true"/>, the method attempts to resolve the server name to an IPv4
        /// address using DNS. If CheckDNS is <see langword="false"/>, the method expects <paramref name="ServerNameOrIP"/> to be a valid IPv4 address
        /// string.</remarks>
        /// <param name="ServerNameOrIP">The server name or IP address to parse. Cannot be <see langword="null"/>, empty, or consist only of white-space characters.</param>
        /// <param name="CheckDNS"><see langword="true"/> to resolve the server name using DNS; <see langword="false"/> to parse the input as a direct IP address.</param>
        /// <exception cref="InvalidOperationException">Thrown if DNS resolution fails when CheckDNS is <see langword="true"/>, or if the input is not a valid IP address when
        /// CheckDNS is <see langword="false"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="ServerNameOrIP"/> is <see langword="null"/>, empty, or consists only of white-space characters.</exception>
        public void ParseServerIP(string ServerNameOrIP, bool CheckDNS = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ServerNameOrIP, nameof(ServerNameOrIP));

            if (CheckDNS)
            {
                // Mit DNS Prüfung
                try
                {
                    this.ServerIP = Dns.GetHostEntry(ServerNameOrIP, AddressFamily.InterNetwork);
                }
                catch (SocketException ex)
                {
                    throw new InvalidOperationException($"Error occured while getting IP-Address for server '{ServerNameOrIP}'.", ex);
                }
            }

            else
            {
                // Ohne DNS Prüfung
                try
                {
                    IPAddress ServerIP = IPAddress.Parse(ServerNameOrIP);

                    this.ServerIP = new()
                    {
                        HostName = ServerNameOrIP,
                        AddressList = [ServerIP]
                    };
                }
                catch (FormatException ex)
                {
                    throw new InvalidOperationException($"The provided server name or IP '{ServerNameOrIP}' is not a valid IP address.", ex);
                }
            }
        }

        /// <summary>
        /// Attempts to parse the specified SQL Server host name or IP address and indicates whether the operation was
        /// successful.
        /// </summary>
        /// <remarks>This method does not throw an exception if parsing fails. Use this method when you
        /// want to test the validity of a SQL Server host name or IP address without handling exceptions.</remarks>
        /// <param name="ServerNameOrIP">The SQL Server host name or IP address to parse. An empty string will results to <see langword="false"/>.</param>
        /// <param name="CheckDNS"><see langword="true"/> to perform a DNS lookup to validate the host name or IP address; otherwise, <see langword="false"/>. The default is
        /// <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the host name or IP address was successfully parsed; otherwise, <see langword="false"/>.</returns>
        public bool TryParseServerIP(string ServerNameOrIP, bool CheckDNS = false)
        {
            try
            {
                this.ParseServerIP(ServerNameOrIP, CheckDNS);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion


        #region Property ServerPort

        /// <summary>
        /// Stores the value of the corresponding property <see cref="ServerPort"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_classInfoLock"/> before accessing this variable.
        /// </remarks>
        private int _serverPort = 1433;

        /// <summary>
        /// Gets or sets the TCP port number used to connect to the SQL Server instance.
        /// </summary>
        /// <remarks>
        /// The port number must be within the valid range defined by <see cref="System.Net.IPEndPoint.MinPort"/> and <see cref="System.Net.IPEndPoint.MaxPort"/>.
        /// On changing this property, the connection check status is reset to <see langword="false"/> and the cached connection string is cleared.
        /// This property is thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if port number is out of the valid range.</exception>
        public int ServerPort
        {
            get
            {
                lock (this._classInfoLock)
                {
                    return this._serverPort;
                }
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, IPEndPoint.MinPort, nameof(this.ServerPort));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, IPEndPoint.MaxPort, nameof(this.ServerPort));

                lock (this._classInfoLock)
                {
                    // Wurde die Port-Nummer geändert?
                    if (value != this._serverPort)
                    {
                        this.IsConnectionChecked = false;
                        this.ConnectionInfo = null;
                        this._serverPort = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        #endregion

        #region Property ServerInstance

        /// <summary>
        /// Stores the value of the corresponding property <see cref="ServerInstance"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_classInfoLock"/> before accessing this variable.
        /// </remarks>
        private string _serverInstance = string.Empty;

        /// <summary>
        /// Gets or sets the name of the SQL Server instance to connect to.
        /// Required only, if multiple instances are hosted on the same server.
        /// Default is an empty string, which indicates the default instance.
        /// </summary>
        /// <remarks>
        /// On changing this property, the connection check status is reset to <see langword="false"/> and the cached connection string is cleared.
        /// This property is thread-safe.
        /// </remarks>
        public string ServerInstance
        {
            get
            {
                lock (this._classInfoLock)
                {
                    return this._serverInstance;
                }
            }
            set
            {
                lock (this._classInfoLock)
                {
                    // Wurde die Instanz geändert?
                    if (value != this._serverInstance)
                    {
                        this.IsConnectionChecked = false;
                        this.ConnectionInfo = null;
                        this._serverInstance = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        #endregion

        #region Property ServerLoginName

        /// <summary>
        /// Stores the value of the corresponding property <see cref="ServerLoginName"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_classInfoLock"/> before accessing this variable.
        /// </remarks>
        private string _serverLoginName = string.Empty;

        /// <summary>
        /// Gets or sets the login name used to authenticate with the SQL Server instance.
        /// </summary>
        /// <remarks>
        /// Only the SQL-Server authentication is supported for login.
        /// The selected login name must have the necessary permissions to access the target database.
        /// On changing this property, the connection check status is reset to <see langword="false"/>, the cached connection string is cleared and the SQL credentials are reset.
        /// This property is thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the provided login name is <see langword="null"/>, empty, or consists only of white-space characters.</exception>
        public string ServerLoginName
        {
            get
            {
                lock (this._classInfoLock)
                {
                    return this._serverLoginName;
                }
            }
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(this.ServerLoginName));

                lock (this._classInfoLock)
                {
                    // Wurde der Login-Name geändert?
                    if (value != this._serverLoginName)
                    {
                        this.IsConnectionChecked = false;
                        this.ConnectionInfo = null;
                        this._serverLoginName = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        #endregion

        #region Property ServerLoginPassword

        /// <summary>
        /// Stores the value of the corresponding property <see cref="ServerLoginPassword"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_classInfoLock"/> before accessing this variable.
        /// </remarks>
        private SecureString _serverLoginPassword = new();

        /// <summary>
        /// Gets or sets the password used for SQL Server login.
        /// </summary>
        /// <remarks>
        /// The password must be provided as a SecureString instance that is marked as read-only.
        /// On changing this property, the connection check status is reset to <see langword="false"/>, the cached connection string is cleared and the SQL credentials are reset.
        /// This property is thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the provided password is not a read-only SecureString.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided password is empty.</exception>"
        public SecureString ServerLoginPassword
        {
            get
            {
                lock (this._classInfoLock)
                {
                    return this._serverLoginPassword;
                }
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value.Length, 1, nameof(this.ServerLoginPassword));

                if (value.IsReadOnly())
                {
                    // Wenn an diese Eigenschaft ein neuer Wert zugewiesen wird,
                    // muss die Verbindung erneut überprüft werden.

                    lock (this._classInfoLock)
                    {
                        this.IsConnectionChecked = false;
                        this.ConnectionInfo = null;
                        this._serverLoginPassword = value;
                        OnPropertyChanged();
                    }
                }
                else
                {
                    throw new ArgumentException("The SQL Server login password must be a read-only SecureString.", nameof(this.ServerLoginPassword));
                }
            }
        }

        #endregion

        #region Property ConnectTimeout

        /// <summary>
        /// Stores the value of the corresponding property <see cref="ConnectTimeout"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_classInfoLock"/> before accessing this variable.
        /// </remarks>
        private TimeSpan _connectTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum amount of time to wait when establishing a connection before timing out.
        /// Valid values are between 1 and 300 seconds, inclusive. The default is 5 seconds.
        /// </summary>
        /// <remarks>
        /// This property is thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is outside the valid range.</exception>
        public TimeSpan ConnectTimeout
        {
            get
            {
                lock (this._classInfoLock)
                {
                    return this._connectTimeout;
                }
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value.TotalSeconds, 1, nameof(this.ConnectTimeout));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.TotalSeconds, 300, nameof(this.ConnectTimeout));

                lock (this._classInfoLock)
                {
                    this.IsConnectionChecked = false;
                    this.ConnectionInfo = null;
                    this._connectTimeout = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Property Database

        /// <summary>
        /// Stores the value of the corresponding property <see cref="Database"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_classInfoLock"/> before accessing this variable.
        /// </remarks>
        private string _database = string.Empty;

        /// <summary>
        /// Gets or sets the name of the database to which the connection applies.
        /// </summary>
        /// <remarks>
        /// On changing this property, the connection check status is reset to <see langword="false"/> and the cached connection string is cleared.
        /// THis property is thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the provided database name is <see langword="null"/>, empty, or consists only of white-space characters.</exception>
        public string Database
        {
            get
            {
                lock (this._classInfoLock)
                {
                    return this._database;
                }
            }
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(this.Database));

                lock (this._classInfoLock)
                {
                    // Datenbankname geändert?
                    if (value != this._database)
                    {
                        this.IsConnectionChecked = false;
                        this.ConnectionInfo = null;
                        this._database = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        #endregion

        #region Property IsConnectionChecked

        /// <summary>
        /// Stores the value of the corresponding property <see cref="IsConnectionChecked"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_connectionInfoLock"/> before accessing this variable.
        /// </remarks>
        private bool _isConnectionChecked = false;

        /// <summary>
        /// Gets a value indicating whether the connection has been successfully checked.
        /// </summary>
        /// <remarks>
        /// Use the method <see cref="CheckConnection"/> to perform the connection check.
        /// The connection check verifies that the provided connection settings are valid and that a connection to the SQL Server can be established.
        /// In case of any changes to the connection settings, this property is automatically reset to <see langword="false"/>.
        /// The property will be also reset to <see langword="false"/> if any execution methods fails during setting up a connection.
        /// This property is thread-safe.
        /// </remarks>
        /// <value>
        /// <see langword="true"/> if the connection has been successfully checked and is ready to use; otherwise, <see langword="false"/>.
        /// </value>
        [MemberNotNullWhen(true, nameof(ConnectionInfo))]
        public bool IsConnectionChecked
        {
            get
            {
                lock (this._connectionInfoLock)
                {
                    return (this._isConnectionChecked && this.ConnectionInfo is not null);
                }
            }

            private set
            {
                lock (this._connectionInfoLock)
                {
                    this._isConnectionChecked = value;
                }
            }
        }

        /// <summary>
        /// Builds and returns a configured SQL Server connection string builder based on the current connection settings.
        /// </summary>
        /// <remarks>
        /// This methos is thread-safe.
        /// </remarks>
        /// <returns>A <see cref="SqlConnectionStringBuilder"/> instance containing the connection parameters for the SQL Server database.</returns>
        /// <param name="IPAddressIndex">The index of the IP address in the <see cref="ServerIP"/> address list to use for the connection string.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="IPAddressIndex"/> is less than 0 or greater than the number of available IP addresses minus one.</exception>
        protected SqlConnectionStringBuilder GetConnectionString(int IPAddressIndex = 0)
        {
            // ###### Wichtiger Hinweis:
            // Damit das Verbindungspooling funktioniert, MUSS immer die gleiche Verbindungszeichenfolge inkl. SqlCredential verwendet werden!
            // Weitere Hinweise: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-connection-pooling

            ArgumentOutOfRangeException.ThrowIfLessThan(IPAddressIndex, 0, nameof(IPAddressIndex));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(IPAddressIndex, this.ServerIP.AddressList.Length - 1, nameof(IPAddressIndex));

            // Temporär alle benötigten Variablen sperren und mit einmal abrufen
            TimeSpan connTimeout;
            IPHostEntry targetIP;
            int port;
            string instance;
            string database;

            lock (this._classInfoLock)
            {
                connTimeout = this.ConnectTimeout;
                targetIP = this.ServerIP;
                port = this.ServerPort;
                instance = this.ServerInstance;
                database = this.Database;
            }

            return new SqlConnectionStringBuilder()
            {
                Authentication = SqlAuthenticationMethod.SqlPassword,
                ConnectTimeout = (int)connTimeout.TotalSeconds,
                DataSource = $"tcp:{targetIP.AddressList[IPAddressIndex]}{(string.IsNullOrEmpty(instance) ? string.Empty : "\\" + instance)},{port}",
                InitialCatalog = database,
                IPAddressPreference = SqlConnectionIPAddressPreference.IPv4First,
                IntegratedSecurity = false,
                PersistSecurityInfo = false,
                TrustServerCertificate = true,
            };
        }

        /// <summary>
        /// Attempts to establish a connection to the SQL Server using the configured login credentials and available IP
        /// addresses. Throws an exception if the connection cannot be established or if required connection settings
        /// are missing.
        /// </summary>
        /// <remarks>This method tests each configured IP address in order until a successful connection
        /// is made. This method is thread-safe.
        /// </remarks>
        /// <exception cref="CheckConnectionException">Thrown if the connection cannot be established due to missing settings,
        /// authentication failures, timeouts, or other SQL-related errors.
        /// Check the <see cref="CheckConnectionException.ExceptionType"/> property for more details on the specific error type.
        /// </exception>
        [MemberNotNull(nameof(ConnectionInfo))]
        public void CheckConnection()
        {
            // Check bereits durchgeführt?
            if (this.IsConnectionChecked && this.ConnectionInfo is not null) return;

            // Temporär die benötigten Variablen abrufen, um thread-safety zu gewährleisten
            string loginName = this.ServerLoginName;
            SecureString loginPwd = this.ServerLoginPassword;
            IPHostEntry targetIP = this.ServerIP;
            SqlCredential cred;

            // Benutzername gesetzt?
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new CheckConnectionException("The SQL Server login name is not set.", CheckConnectionExceptionType.NoLoginName);
            }

            // Passwort gesetzt?
            if (loginPwd.Length == 0)
            {
                throw new CheckConnectionException("The SQL Server login password is not set.", CheckConnectionExceptionType.NoPassword);
            }

            // Mindestens eine IP-Adresse vorhanden?
            if (targetIP.AddressList is null || targetIP.AddressList.Length == 0)
            {
                throw new CheckConnectionException("No valid IP address is set for the SQL Server connection.", CheckConnectionExceptionType.NoServerIP);
            }

            // Credential vorbereiten
            cred = new SqlCredential(loginName, loginPwd);

            // Verfügbare IP-Adressen durchgehen und Verbindung testen
            for (int i = 0; i < targetIP.AddressList.Length; i++)
            {
                SqlConnectionStringBuilder connBuilder = this.GetConnectionString(i);
                SqlConnInfo connInfo = new(connBuilder.ConnectionString, cred);

                try
                {
                    // Zuerst prüfen, ob diese IP-Adresse und Port-Nummer überhaupt erreichbar ist
                    using TcpClient tcpClient = new();
                    try
                    {
                        tcpClient.Connect(targetIP.AddressList[i], this.ServerPort);
                    }
                    catch (SocketException)
                    {
                        // Verbindungsfehler auf TCP-Ebene, z.B. Zeitüberschreitung oder Zielhost nicht erreichbar
                        throw new CheckConnectionException($"Unable to connect to the SQL Server '{targetIP.AddressList[i]}:{this.ServerPort}'.", CheckConnectionExceptionType.ConnectionTimeout);
                    }

                    // Versuche die Verbindung zu öffnen
                    using SqlConnection sqlConn = OpenConnection(connInfo);

                    // Verbindung erfolgreich
                    // Interne Variablen setzen
                    // SqlCredential muss für das Connection Pooling immer die gleiche Instanz sein!
                    // Daher hier speichern.
                    lock (this._connectionInfoLock)
                    {
                        this.ConnectionInfo = connInfo;
                        this.IsConnectionChecked = true;
                    }

                    return;
                }
                catch (CheckConnectionException ex) when (ex.ExceptionType == CheckConnectionExceptionType.ConnectionTimeout && i < targetIP.AddressList.Length - 1)
                {
                    // Verbindungszeitüberschreitung, aber es sind noch weitere IP-Adressen vorhanden
                    // Daher nächste IP-Adresse versuchen
                    continue;
                }
                catch (CheckConnectionException)
                {
                    // Bereits eine bekannte Exception, daher weiterwerfen
                    throw;
                }
            }

            // Wenn bis hierher gelangt, war keine Verbindung erfolgreich
            // Dieser Fall sollte eigentlich nie eintreten, da alle Fehler bereits oben abgefangen werden.
            throw new CheckConnectionException("Unable to establish a connection to the SQL Server with the provided settings.", CheckConnectionExceptionType.UnhandledException);
        }

        /// <summary>
        /// Asynchronously attempts to establish a connection to the SQL Server using the configured login credentials and available IP
        /// addresses. Throws an exception if the connection cannot be established or if required connection settings
        /// are missing.
        /// </summary>
        /// <remarks>
        /// This method tests each configured IP address in order until a successful connection
        /// is made. This method is thread-safe.
        /// </remarks>
        /// <param name="cancellation">An optional cancellation token that can be used to cancel the operation. If null, the operation cannot be
        /// cancelled.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="CheckConnectionException">Thrown if the connection cannot be established due to missing settings,
        /// authentication failures, timeouts, or other SQL-related errors.
        /// Check the <see cref="CheckConnectionException.ExceptionType"/> property for more details on the specific error type.
        /// </exception>
        public async Task CheckConnectionAsync(CancellationToken? cancellation = null)
        {
            // Sofort abbrechen, wenn das CancellationToken bereits zu Beginn gesetzt ist
            if (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                throw new CheckConnectionException("The connection check operation was canceled before it started.", CheckConnectionExceptionType.RequestCancelled);
            }

            // Check bereits durchgeführt?
            if (this.IsConnectionChecked && this.ConnectionInfo is not null) return;

            // Temporär die benötigten Variablen abrufen, um thread-safety zu gewährleisten
            string loginName = this.ServerLoginName;
            SecureString loginPwd = this.ServerLoginPassword;
            IPHostEntry targetIP = this.ServerIP;
            SqlCredential cred;

            // Benutzername gesetzt?
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new CheckConnectionException("The SQL Server login name is not set.", CheckConnectionExceptionType.NoLoginName);
            }

            // Passwort gesetzt?
            if (loginPwd.Length == 0)
            {
                throw new CheckConnectionException("The SQL Server login password is not set.", CheckConnectionExceptionType.NoPassword);
            }

            // Mindestens eine IP-Adresse vorhanden?
            if (targetIP.AddressList is null || targetIP.AddressList.Length == 0)
            {
                throw new CheckConnectionException("No valid IP address is set for the SQL Server connection.", CheckConnectionExceptionType.NoServerIP);
            }

            // Credential vorbereiten
            cred = new SqlCredential(loginName, loginPwd);

            // Verfügbare IP-Adressen durchgehen und Verbindung testen
            for (int i = 0; i < targetIP.AddressList.Length; i++)
            {
                SqlConnectionStringBuilder connBuilder = this.GetConnectionString(i);
                SqlConnInfo connInfo = new(connBuilder.ConnectionString, cred);

                try
                {
                    // Zuerst prüfen, ob diese IP-Adresse und Port-Nummer überhaupt erreichbar ist
                    using TcpClient tcpClient = new();

                    try
                    {
                        await tcpClient.ConnectAsync(targetIP.AddressList[i], this.ServerPort, cancellation.GetValueOrDefault());
                    }
                    catch (SocketException)
                    {
                        throw new CheckConnectionException($"Unable to connect to the SQL Server '{targetIP.AddressList[i]}:{this.ServerPort}'.", CheckConnectionExceptionType.ConnectionTimeout);
                    }
                    catch (OperationCanceledException)
                    {
                        // Abbruch angefordert
                        throw new CheckConnectionException("The connection to the SQL Server was canceled.", CheckConnectionExceptionType.RequestCancelled);
                    }

                    // Versuche die Verbindung zu öffnen
                    using SqlConnection sqlConn = await OpenConnectionAsync(cancellation, connInfo);

                    // Verbindung erfolgreich
                    // Interne Variablen setzen
                    // SqlCredential muss für das Connection Pooling immer die gleiche Instanz sein!
                    // Daher hier speichern.
                    lock (this._connectionInfoLock)
                    {
                        this.ConnectionInfo = connInfo;
                        this.IsConnectionChecked = true;
                    }

                    return;
                }
                catch (CheckConnectionException ex) when (ex.ExceptionType == CheckConnectionExceptionType.ConnectionTimeout && i < targetIP.AddressList.Length - 1)
                {
                    // Verbindungszeitüberschreitung, aber es sind noch weitere IP-Adressen vorhanden
                    // Daher nächste IP-Adresse versuchen
                    continue;
                }
                catch (CheckConnectionException)
                {
                    // Bereits eine bekannte Exception, daher weiterwerfen
                    throw;
                }
                catch (Exception) when (cancellation.GetValueOrDefault().IsCancellationRequested)
                {
                    // Abbruch angefordert
                    throw new CheckConnectionException("The connection to the SQL Server was canceled.", CheckConnectionExceptionType.RequestCancelled);
                }
            }

            // Wenn bis hierher gelangt, war keine Verbindung erfolgreich
            // Dieser Fall sollte eigentlich nie eintreten, da alle Fehler bereits oben abgefangen werden.
            throw new CheckConnectionException("Unable to establish a connection to the SQL Server with the provided settings.", CheckConnectionExceptionType.UnhandledException);
        }

        #endregion

        #region Property ConnectionInfo

        /// <summary>
        /// Stores the value of the corresponding property <see cref="ConnectionInfo"/>.
        /// </summary>
        /// <remarks>
        /// Lock must be acquired on <see cref="_connectionInfoLock"/> before accessing this variable.
        /// </remarks>
        private SqlConnInfo? _connectionInfo = null;

        /// <summary>
        /// Gets the current SQL connection information associated with the instance.
        /// </summary>
        /// <remarks>Access to this property is thread-safe. The returned value may be <see langword="null"/> if no
        /// connection information has been set.</remarks>
        protected SqlConnInfo? ConnectionInfo
        {
            get
            {
                lock (this._connectionInfoLock)
                {
                    return this._connectionInfo;
                }
            }

            private set
            {
                lock (this._connectionInfoLock)
                {
                    if (value is null)
                    {
                        this.IsConnectionChecked = false;
                    }
                    this._connectionInfo = value;
                }
            }
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
        /// </summary>
        /// <remarks>This method is called by public Dispose methods and the finalizer. When disposing is
        /// true, this method releases all resources held by managed objects. When disposing is false, only unmanaged
        /// resources are released. Override this method to release additional resources in a derived class.</remarks>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    this._serverLoginPassword.Dispose();
                }
                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                this.ConnectionInfo = null;
                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SetupSqlConnection"/> class.
        /// </summary>
        ~SetupSqlConnection()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the class.
        /// </summary>
        /// <remarks>Call this method when you are finished using the object to release unmanaged
        /// resources and perform other cleanup operations. After calling Dispose, the object should not be used
        /// further.</remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region INotifyPropertyChanged implementations

        /// <summary>
        /// Occurs when a property value changes. This event is raised whenever a property that affects the connection settings is modified,
        /// allowing UI components or other observers to react to changes in connection settings or status.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event to notify listeners that a property value has changed.
        /// </summary>
        /// <remarks>Call this method within property setters to ensure that data bindings and observers
        /// are updated when a property's value changes. This is commonly used in classes that implement
        /// INotifyPropertyChanged to support property change notification.</remarks>
        /// <param name="propertyName">The name of the property that changed. If not specified, the name of the caller member is used.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Synchron execution methods

        /// <summary>
        /// Opens a new SQL Server database connection.
        /// </summary>
        /// <remarks>The caller is responsible for closing and disposing the returned <see cref="SqlConnection"/> when it is no longer needed.
        /// This method translates common SQL Server connection errors into <see cref="CheckConnectionException"/> to provide more specific error information.</remarks>
        /// <param name="testConn">
        /// Optional. The connection information to use for opening the SQL Server connection.
        /// This property should be only used for testing purposes.
        /// By default this method uses the connection information stored in the instance.
        /// </param>
        /// <returns>A new instance of <see cref="SqlConnection"/> that represents an open connection to the SQL Server database.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no connection information is available to establish a connection.</exception>
        /// <exception cref="CheckConnectionException">Thrown if an error occurs while attempting to connect to the SQL Server, such as connection timeouts,
        /// authentication failures, or other SQL-related errors.</exception>
        protected SqlConnection OpenConnection(SqlConnInfo? testConn = null)
        {
            // Verbindungsinformation abrufen
            SqlConnInfo connInfo = testConn ?? this.ConnectionInfo ?? throw new InvalidOperationException("No connection information is available.");

            SqlConnection sqlConn = new(connInfo.ConnectionString, connInfo.Credential);
            // Verbindung öffnen
            try
            {
                sqlConn.Open();
            }
            // Die Fehlernummern vom SQL-Server sind hier gelistet:
            // https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors

            // Bei allen Verbindungsfehlern soll die IsConnectionChecked-Eigenschaft auf false gesetzt werden
            // Nachfolgende Fehlernummern sind typisch für Verbindungsprobleme
            catch (SqlException ex) when (ex.Number == 258 && ex.Class == 20)
            {
                // Verbindungszeitüberschreitung
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The connection to the SQL Server timed out.", CheckConnectionExceptionType.ConnectionTimeout, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                // Keine Datenbankzugriffsrechte
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The login account does not have access to the specified database.", CheckConnectionExceptionType.NoDatabaseAccess, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 18456)
            {
                // Falscher Benutzername oder Passwort
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The SQL Server login name or password is incorrect.", CheckConnectionExceptionType.WrongLoginNameOrPassword, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 18470)
            {
                // Konto gesperrt
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The SQL Server login account is disabled.", CheckConnectionExceptionType.SqlAccountDisabled, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 18487)
            {
                // Passwort abgelaufen
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The password of the login account has expired.", CheckConnectionExceptionType.PasswordExpired, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 18488)
            {
                // Passwort muss zurückgesetzt werden
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The password of the login account must be changed.", CheckConnectionExceptionType.PasswordChangeRequired, ex.Number, ex);
            }
            catch (SqlException ex)
            {
                // Andere SQL-Fehler
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("An SQL error occurred while attempting to connect to the SQL Server.", CheckConnectionExceptionType.UnhandledException, ex.Number, ex);
            }
            catch (Exception ex)
            {
                // Andere Fehler
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("An unhandled exception occurred while attempting to connect to the SQL Server.", ex);
            }

            return sqlConn;
        }

        /// <summary>
        /// Executes a SQL statement against the database and returns the number of rows affected.
        /// </summary>
        /// <param name="command">The <see cref="SqlCommand"/> to execute. The command must be fully configured with the desired SQL statement.</param>
        /// <returns>The number of rows affected by the SQL statement. For UPDATE, INSERT, and DELETE statements, this is the
        /// number of rows affected. For other types of statements, -1 may be returned.</returns>
        /// <exception cref="CheckConnectionException">Thrown if there is an error establishing the database connection.</exception>
        /// <exception cref="SqlException">Thrown if there is an error executing the SQL command.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an unhandled error occurs while executing the SQL command.</exception>
        public int ExecNonQuery(SqlCommand command)
        {
            try
            {
                // Korrekte Verbindung sicherstellen
                if (!this.IsConnectionChecked)
                {
                    this.CheckConnection();
                }

                // Verbindung herstellen
                using SqlConnection sqlConn = OpenConnection();

                command.Connection = sqlConn;

                return command.ExecuteNonQuery();
            }
            catch (CheckConnectionException)
            {
                // Diese Exception wird beim Öffnen der Verbindung geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (SqlException)
            {
                // Diese Exception wird beim Ausführen des Befehls geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while executing the SQL command.", ex);
            }
        }

        /// <summary>
        /// Executes the specified SQL command and returns the first column of the first row in the result set.
        /// </summary>
        /// <param name="command">The <see cref="SqlCommand"/> to execute. The command text and parameters must be set before calling this
        /// method.</param>
        /// <returns>An object representing the value of the first column of the first row in the result set, or <see
        /// langword="null"/> if the result set is empty.</returns>
        /// <exception cref="CheckConnectionException">Thrown if there is an error establishing the database connection.</exception>
        /// <exception cref="SqlException">Thrown if there is an error executing the SQL command.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an unhandled error occurs while executing the SQL command.</exception>
        public object? ExecScalar(SqlCommand command)
        {
            try
            {
                // Korrekte Verbindung sicherstellen
                if (!this.IsConnectionChecked)
                {
                    this.CheckConnection();
                }

                // Verbindung herstellen
                using SqlConnection sqlConn = OpenConnection();

                command.Connection = sqlConn;

                return command.ExecuteScalar();
            }
            catch (CheckConnectionException)
            {
                // Diese Exception wird beim Öffnen der Verbindung geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (SqlException)
            {
                // Diese Exception wird beim Ausführen des Befehls geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while executing the SQL command.", ex);
            }
        }

        /// <summary>
        /// Executes the specified SQL command and returns a <see cref="SqlDataReader"/> for reading the results.
        /// </summary>
        /// <remarks>The <see cref="SqlDataReader"/> returned by this method is configured to automatically close the
        /// underlying SqlConnection when the reader is closed. Ensure that the SqlDataReader is properly disposed to
        /// release database resources.</remarks>
        /// <param name="command">The <see cref="SqlCommand"/> to execute. The command text and parameters must be set before calling this method.</param>
        /// <returns>A <see cref="SqlDataReader"/> object for reading the results of the executed command. The caller is responsible for
        /// closing the SqlDataReader when finished, which will also close the underlying connection.</returns>
        /// <exception cref="CheckConnectionException">Thrown if there is an error establishing the database connection.</exception>
        /// <exception cref="SqlException">Thrown if there is an error executing the SQL command.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an unhandled error occurs while executing the SQL command.</exception>
        public SqlDataReader ExecReader(SqlCommand command)
        {
            try
            {
                // Korrekte Verbindung sicherstellen
                if (!this.IsConnectionChecked)
                {
                    this.CheckConnection();
                }

                // Verbindung herstellen
                SqlConnection sqlConn = OpenConnection();

                command.Connection = sqlConn;

                // Die Verbindung wird erst geschlossen, wenn der SqlDataReader geschlossen wird
                return command.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
            }
            catch (CheckConnectionException)
            {
                // Diese Exception wird beim Öffnen der Verbindung geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (SqlException)
            {
                // Diese Exception wird beim Ausführen des Befehls geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while executing the SQL command.", ex);
            }
        }

        #endregion

        #region Asynchron execution methods

        /// <summary>
        /// Asynchronously opens a new SQL Server database connection.
        /// </summary>
        /// <remarks>The returned SqlConnection is already open when the task completes successfully.
        /// The caller is responsible for disposing the connection when it is no longer needed.
        /// This method translates common SQL Server connection errors into <see cref="CheckConnectionException"/> to provide more specific error information.
        /// </remarks>
        /// <param name="cancellation">A cancellation token that can be used to cancel the connection attempt. If not specified, the operation
        /// cannot be canceled.</param>
        /// <param name="testConn">
        /// Optional. The connection information to use for opening the SQL Server connection.
        /// This property should be only used for testing purposes.
        /// By default this method uses the connection information stored in the instance.
        /// </param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains an open <see cref="SqlConnection"/> instance.</returns>
        /// <exception cref="CheckConnectionException">Thrown if the connection attempt fails due to SQL Server errors, authentication issues, timeouts,
        /// cancellation, or other connection-related problems.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no connection information is available to establish the connection.</exception>
        protected async Task<SqlConnection> OpenConnectionAsync(CancellationToken? cancellation = null, SqlConnInfo? testConn = null)
        {
            // Sofort abbrechen, wenn das CancellationToken bereits zu Beginn gesetzt ist
            if (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                throw new CheckConnectionException("The connection check operation was canceled before it started.", CheckConnectionExceptionType.RequestCancelled);
            }

            // Verbindungsinformationen abrufen
            SqlConnInfo connInfo = testConn ?? this.ConnectionInfo ?? throw new InvalidOperationException("No connection information is available.");

            SqlConnection sqlConn = new(connInfo.ConnectionString, connInfo.Credential);
            // Verbindung öffnen
            try
            {
                await sqlConn.OpenAsync(cancellation ?? CancellationToken.None);
            }
            // Die Fehlernummern vom SQL-Server sind hier gelistet:
            // https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors

            // Bei allen Verbindungsfehlern soll die IsConnectionChecked-Eigenschaft auf false gesetzt werden
            // Nachfolgende Fehlernummern sind typisch für Verbindungsprobleme
            catch (SqlException ex) when (ex.Number == 258 && ex.Class == 20)
            {
                // Verbindungszeitüberschreitung
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The connection to the SQL Server timed out.", CheckConnectionExceptionType.ConnectionTimeout, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                // Keine Datenbankzugriffsrechte
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The login account does not have access to the specified database.", CheckConnectionExceptionType.NoDatabaseAccess, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 18456)
            {
                // Falscher Benutzername oder Passwort
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The SQL Server login name or password is incorrect.", CheckConnectionExceptionType.WrongLoginNameOrPassword, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 18470)
            {
                // Konto gesperrt
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The SQL Server login account is disabled.", CheckConnectionExceptionType.SqlAccountDisabled, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 18487)
            {
                // Passwort abgelaufen
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The password of the login account has expired.", CheckConnectionExceptionType.PasswordExpired, ex.Number, ex);
            }
            catch (SqlException ex) when (ex.Number == 18488)
            {
                // Passwort muss zurückgesetzt werden
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("The password of the login account must be changed.", CheckConnectionExceptionType.PasswordChangeRequired, ex.Number, ex);
            }
            catch (SqlException ex)
            {
                // Andere SQL-Fehler
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("An SQL error occurred while attempting to connect to the SQL Server.", CheckConnectionExceptionType.UnhandledException, ex.Number, ex);
            }
            catch (Exception) when (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                // Abbruch angefordert
                // Hierbei wird kein IsConnectionChecked auf false gesetzt, da die Verbindung ja nicht fehlgeschlagen ist
                throw new CheckConnectionException("The connection to the SQL Server was canceled.", CheckConnectionExceptionType.RequestCancelled);
            }
            catch (Exception ex)
            {
                // Andere Fehler
                this.IsConnectionChecked = false;
                throw new CheckConnectionException("An unhandled exception occurred while attempting to connect to the SQL Server.", ex);
            }

            return sqlConn;

        }

        /// <summary>
        /// Asynchronously executes a non-query SQL command using the specified <see cref="SqlCommand"/> and returns the
        /// number of rows affected.
        /// </summary>
        /// <param name="command">The <see cref="SqlCommand"/> to execute. The command must be properly configured with the desired SQL
        /// statement and any required parameters.</param>
        /// <param name="cancellation">An optional <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected by
        /// the command.</returns>
        /// <exception cref="CheckConnectionException">Thrown if there is an error establishing the database connection.</exception>
        /// <exception cref="SqlException">Thrown if there is an error executing the SQL command.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an unhandled error occurs while executing the SQL command.</exception>
        public async Task<int> ExecNonQueryAsync(SqlCommand command, CancellationToken? cancellation = null)
        {
            // Sofort abbrechen, wenn das CancellationToken bereits zu Beginn gesetzt ist
            if (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                throw new CheckConnectionException("The connection check operation was canceled before it started.", CheckConnectionExceptionType.RequestCancelled);
            }

            try
            {
                // Korrekte Verbindung sicherstellen
                if (!this.IsConnectionChecked)
                {
                    await this.CheckConnectionAsync(cancellation);
                }

                // Verbindung herstellen
                using SqlConnection sqlConn = await OpenConnectionAsync(cancellation);

                command.Connection = sqlConn;

                return await command.ExecuteNonQueryAsync(cancellation ?? CancellationToken.None);
            }
            catch (CheckConnectionException)
            {
                // Diese Exception wird beim Öffnen der Verbindung geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception) when (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                // Abbruch angefordert
                throw new CheckConnectionException("The execution of the SQL command was canceled.", CheckConnectionExceptionType.RequestCancelled);
            }
            catch (SqlException)
            {
                // Diese Exception wird beim Ausführen des Befehls geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while executing the SQL command.", ex);
            }
        }

        /// <summary>
        /// Asynchronously executes the specified SQL command and returns the first column of the first row in the
        /// result set.
        /// </summary>
        /// <param name="command">The <see cref="SqlCommand"/> to execute. The command must be fully configured except for its connection,
        /// which will be set by this method.</param>
        /// <param name="cancellation">An optional <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the value of the first column of
        /// the first row in the result set, or <see langword="null"/> if the result set is empty.
        /// </returns>
        /// <exception cref="CheckConnectionException">Thrown if there is an error establishing the database connection.</exception>
        /// <exception cref="SqlException">Thrown if there is an error executing the SQL command.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an unhandled error occurs while executing the SQL command.</exception>
        public async Task<object?> ExecScalarAsync(SqlCommand command, CancellationToken? cancellation = null)
        {
            // Sofort abbrechen, wenn das CancellationToken bereits zu Beginn gesetzt ist
            if (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                throw new CheckConnectionException("The connection check operation was canceled before it started.", CheckConnectionExceptionType.RequestCancelled);
            }

            try
            {
                // Korrekte Verbindung sicherstellen
                if (!this.IsConnectionChecked)
                {
                    await this.CheckConnectionAsync(cancellation);
                }

                // Verbindung herstellen
                using SqlConnection sqlConn = await OpenConnectionAsync(cancellation);

                command.Connection = sqlConn;

                return await command.ExecuteScalarAsync(cancellation ?? CancellationToken.None);
            }
            catch (CheckConnectionException)
            {
                // Diese Exception wird beim Öffnen der Verbindung geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception) when (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                // Abbruch angefordert
                throw new CheckConnectionException("The execution of the SQL command was canceled.", CheckConnectionExceptionType.RequestCancelled);
            }
            catch (SqlException)
            {
                // Diese Exception wird beim Ausführen des Befehls geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while executing the SQL command.", ex);
            }
        }

        /// <summary>
        /// Asynchronously executes the specified SQL command and returns a <see cref="SqlDataReader"/> for reading the results.
        /// </summary>
        /// <remarks>The <see cref="SqlDataReader"/> returned by this method is configured to automatically close the
        /// underlying SqlConnection when the reader is closed. Ensure that the SqlDataReader is properly disposed to
        /// release database resources</remarks>
        /// <param name="command">The <see cref="SqlCommand"/> to execute. The command text and parameters must be set before calling this method.</param>
        /// <param name="cancellation">An optional <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains a <see cref="SqlDataReader"/> for reading the
        /// results of the command. The associated connection is closed when the SqlDataReader is closed.</returns>
        /// <exception cref="CheckConnectionException">Thrown if there is an error establishing the database connection.</exception>
        /// <exception cref="SqlException">Thrown if there is an error executing the SQL command.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an unhandled error occurs while executing the SQL command.</exception>
        public async Task<SqlDataReader> ExecReaderAsync(SqlCommand command, CancellationToken? cancellation = null)
        {
            // Sofort abbrechen, wenn das CancellationToken bereits zu Beginn gesetzt ist
            if (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                throw new CheckConnectionException("The connection check operation was canceled before it started.", CheckConnectionExceptionType.RequestCancelled);
            }

            try
            {
                // Korrekte Verbindung sicherstellen
                if (!this.IsConnectionChecked)
                {
                    await this.CheckConnectionAsync(cancellation);
                }

                // Verbindung herstellen
                SqlConnection sqlConn = await OpenConnectionAsync(cancellation);

                command.Connection = sqlConn;

                // Die Verbindung wird erst geschlossen, wenn der SqlDataReader geschlossen wird
                SqlDataReader reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection, cancellation ?? CancellationToken.None);

                // Prüfen, ob der Abbruch während des Ausführens angefordert wurde
                if (cancellation.GetValueOrDefault().IsCancellationRequested)
                {
                    reader.Close(); // Reader und damit auch die Verbindung schließen
                    throw new CheckConnectionException("The execution of the SQL command was canceled.", CheckConnectionExceptionType.RequestCancelled);
                }
                return reader;
            }
            catch (CheckConnectionException)
            {
                // Diese Exception wird beim Öffnen der Verbindung geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception) when (cancellation.GetValueOrDefault().IsCancellationRequested)
            {
                // Abbruch angefordert
                throw new CheckConnectionException("The execution of the SQL command was canceled.", CheckConnectionExceptionType.RequestCancelled);
            }
            catch (SqlException)
            {
                // Diese Exception wird beim Ausführen des Befehls geworfen und soll nur weitergereicht werden
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while executing the SQL command.", ex);
            }
        }

        #endregion
    }
}
