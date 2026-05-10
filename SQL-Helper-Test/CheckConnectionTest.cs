using Microsoft.Data.SqlClient;
using System.Data;
using System.Security;

namespace BiemannT.SQLHelper.Test
{
    [TestClass]
    public sealed class CheckConnectionTest
    {
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void SetupDatabase(TestContext testContext)
        {
            // Um in dieser Klasse alle Tests durchführen zu können, werden hierzu im SQL-Server eine Testdatenbank und verschiedene Testbenutzer erstellt.
            // Die Verbindung zum SQL-Server sollte so allgemein wie möglich sein, um auch die Tests in einer GitHub-Action zu ermöglichen.

            string[] sqlCmds =
            [
                // Aktiver, gültiger Login
                "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE [name] = 'ActiveUser') DROP LOGIN ActiveUser;",
                "CREATE LOGIN ActiveUser WITH PASSWORD = 'veryG88dPassword';",
                // Login mit Anweisung zum ändern des Passwortes
                "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE [name] = 'UserPwdChange') DROP LOGIN UserPwdChange;",
                "CREATE LOGIN UserPwdChange WITH PASSWORD = 'onl4Standard' MUST_CHANGE, CHECK_EXPIRATION = ON;",
                // Inaktiver Login
                "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE [name] = 'DisabledUser') DROP LOGIN DisabledUser;",
                "CREATE LOGIN DisabledUser WITH PASSWORD = 'NotN55ded';",
                "ALTER LOGIN DisabledUser DISABLE;",
                // Beispieldatenbank
                "DROP DATABASE IF EXISTS TestDB;",
                "CREATE DATABASE TestDB;",
                // Login mit Berechtigung zur Anmeldung an Datenbank
                "IF EXISTS (SELECT 1 FROM sys.server_principals WHERE [name] = 'UserWithPermission') DROP LOGIN UserWithPermission;",
                "CREATE LOGIN UserWithPermission WITH PASSWORD = 'Allw6dUser';",
                "USE TestDB;",
                "CREATE USER DbUser FOR LOGIN UserWithPermission;"
            ];

            SqlConnectionStringBuilder connBuilder = new()
            {
                Authentication = SqlAuthenticationMethod.SqlPassword,
                ConnectTimeout = 5,
                DataSource = "tcp:127.0.0.1,1433",
                InitialCatalog = "master",
                IPAddressPreference = SqlConnectionIPAddressPreference.IPv4First,
                IntegratedSecurity = false,
                PersistSecurityInfo = false,
                TrustServerCertificate = true
            };

            string saPwd = "Sql2Admin";
            SecureString saSecure = new();
            foreach (char c in saPwd)
            {
                saSecure.AppendChar(c);
            }
            saSecure.MakeReadOnly();

            SqlCredential sqlCred = new("sa", saSecure);

            using SqlConnection sqlConn = new(connBuilder.ConnectionString, sqlCred);

            sqlConn.Open();

            foreach (string cmd in sqlCmds)
            {
                SqlCommand command = new()
                {
                    CommandType = System.Data.CommandType.Text,
                    CommandText = cmd,
                    CommandTimeout = 10,
                    Connection = sqlConn
                };

                command.ExecuteNonQuery();
            }
            testContext.WriteLine("Database created for tests.");
        }

        [DataRow("", "", "", 0, "", 1)] // Kein Loginname
        [DataRow("unknown", "", "", 0, "", 2)] // Kein Passwort
        [DataRow("unknown", "unknown", "", 0, "", 5)] // Kein Hostname
        [DataRow("unknown", "unknown", "localhost", 1430, "", 8)] // Falscher Port
        [DataRow("unknown", "unknown", "localhost", 1433, "", 7)] // Falscher Loginname
        [DataRow("sa", "unknown", "localhost", 1433, "", 7)] // Falsches Passwort
        [DataRow("ActiveUser", "veryG88dPassword", "localhost", 1433, "", -1)] // Gültiger Login
        [DataRow("UserPwdChange", "onl4Standard", "localhost", 1433, "", 4)] // Login mit Aufforderung zum Passwortwechsel
        [DataRow("DisabledUser", "NotN55ded", "localhost", 1433, "", 6)] // Inaktiver Login
        [DataRow("ActiveUser", "veryG88dPassword", "localhost", 1433, "TestDB", 9)] // Benutzer ohne Berechtigung zur Anmeldung an Datenbank
        [DataRow("UserWithPermission", "Allw6dUser", "localhost", 1433, "TestDB", -1)] // Benutzer mit Berechtigung zur Anmeldung an Datenbank
        [TestMethod]
        public void Test_CheckConnection(string loginName, string loginPassword, string hostName, int serverPort, string database, int expectedErrorType)
        {
            // Unbekannte Hostnamen werden im Test SqlConnectionTest.Test_ParseSQLServerIP() geprüft.
            SetupSqlConnection sqlConnection = new();

            // Werte setzen, wenn angegeben
            // Login Name
            if (!string.IsNullOrEmpty(loginName))
            {
                sqlConnection.ServerLoginName = loginName;
            }

            // Login Passwort
            if (!string.IsNullOrEmpty(loginPassword))
            {
                SecureString securePassword = new();
                foreach (char c in loginPassword)
                {
                    securePassword.AppendChar(c);
                }
                securePassword.MakeReadOnly();
                sqlConnection.ServerLoginPassword = securePassword;
            }

            // Host Name
            if (!string.IsNullOrEmpty(hostName))
            {
                sqlConnection.ParseServerIP(hostName, true);
            }

            // Server Port
            if (serverPort > 0)
            {
                sqlConnection.ServerPort = serverPort;
            }

            // Datenbank
            if (!string.IsNullOrEmpty(database))
            {
                sqlConnection.Database = database;
            }

            try
            {
                // Versuche die Verbindung herzustellen
                TestContext.WriteLine("Versuche die Verbindung herzustellen mit folgenden Einstellungen:");
                TestContext.WriteLine($"  Server IP: {sqlConnection.ServerIP.HostName}");
                TestContext.WriteLine($"  Server Port: {sqlConnection.ServerPort}");
                TestContext.WriteLine($"  Datenbank: {sqlConnection.Database}");
                TestContext.WriteLine($"  Login Name: {sqlConnection.ServerLoginName}");
                TestContext.WriteLine($"  Login Passwort: {loginPassword}");

                sqlConnection.CheckConnection();

                if (expectedErrorType < 0)
                {
                    // Wie erwartet ist kein Fehler aufgetreten
                    TestContext.WriteLine("Verbindung wie erwartet erfolgreich hergestellt.");
                }
                else
                {
                    // Es wurde ein Fehler erwartet, aber keiner ist aufgetreten
                    Assert.Fail("Es wurde ein Fehler erwartet, aber die Verbindung konnte erfolgreich hergestellt werden.");
                }
            }
            catch (CheckConnectionException ex)
            {
                TestContext.WriteLine($"Fehler beim Verbindungsaufbau: {ex.Message} (Fehlertyp: {ex.ExceptionType} / Original Fehlernummer: {ex.SqlErrorNumber})");

                if ((int)ex.ExceptionType != expectedErrorType)
                {
                    // Der aufgetretene Fehler entspricht nicht dem erwarteten Fehler
                    Assert.Fail($"Es wurde ein Fehler vom Typ {expectedErrorType} erwartet, aber es ist ein Fehler vom Typ {ex.ExceptionType} aufgetreten.");
                }
                else
                {
                    // Der aufgetretene Fehler entspricht dem erwarteten Fehler
                    TestContext.WriteLine("Der aufgetretene Fehler entspricht dem erwarteten Fehler.");
                }
            }
        }

        [DataRow("", "", "", 0, "", 1, 10)] // Kein Loginname
        [DataRow("unknown", "", "", 0, "", 2, 10)] // Kein Passwort
        [DataRow("unknown", "unknown", "", 0, "", 5, 10)] // Kein Hostname
        [DataRow("unknown", "unknown", "localhost", 1430, "", 8, 10)] // Falscher Port
        [DataRow("unknown", "unknown", "localhost", 1433, "", 7, 10)] // Falscher Loginname
        [DataRow("sa", "unknown", "localhost", 1433, "", 7, 10)] // Falsches Passwort
        [DataRow("ActiveUser", "veryG88dPassword", "localhost", 1433, "", -1, 10)] // Gültiger Login
        [DataRow("UserPwdChange", "onl4Standard", "localhost", 1433, "", 4, 10)] // Login mit Aufforderung zum Passwortwechsel
        [DataRow("DisabledUser", "NotN55ded", "localhost", 1433, "", 6, 10)] // Inaktiver Login
        [DataRow("ActiveUser", "veryG88dPassword", "localhost", 1433, "TestDB", 9, 10)] // Benutzer ohne Berechtigung zur Anmeldung an Datenbank
        [DataRow("UserWithPermission", "Allw6dUser", "localhost", 1433, "TestDB", -1, 10)] // Benutzer mit Berechtigung zur Anmeldung an Datenbank
        [DataRow("ActiveUser", "veryG88dPassword", "localhost", 1433, "", 10, 0)] // Sofortiger Abbruch
        [DataRow("unknown", "unknown", "localhost", 1430, "", 10, 2)] // Abbruch vor dem Connection Timeout
        [TestMethod]
        public async Task Test_CheckConnectionAsync(string loginName, string loginPassword, string hostName, int serverPort, string database, int expectedErrorType, int cancellationTime)
        {
            // Unbekannte Hostnamen werden im Test SqlConnectionTest.Test_ParseSQLServerIP() geprüft.
            SetupSqlConnection sqlConnection = new();

            // Werte setzen, wenn angegeben
            // Login Name
            if (!string.IsNullOrEmpty(loginName))
            {
                sqlConnection.ServerLoginName = loginName;
            }

            // Login Passwort
            if (!string.IsNullOrEmpty(loginPassword))
            {
                SecureString securePassword = new();
                foreach (char c in loginPassword)
                {
                    securePassword.AppendChar(c);
                }
                securePassword.MakeReadOnly();
                sqlConnection.ServerLoginPassword = securePassword;
            }

            // Host Name
            if (!string.IsNullOrEmpty(hostName))
            {
                sqlConnection.ParseServerIP(hostName, true);
            }

            // Server Port
            if (serverPort > 0)
            {
                sqlConnection.ServerPort = serverPort;
            }

            // Datenbank
            if (!string.IsNullOrEmpty(database))
            {
                sqlConnection.Database = database;
            }

            try
            {
                // Abbruch nach vorgegebenen Timeout
                CancellationTokenSource cts = new();
                cts.CancelAfter(cancellationTime * 1000);

                // Versuche die Verbindung herzustellen
                TestContext.WriteLine("Versuche die Verbindung herzustellen mit folgenden Einstellungen:");
                TestContext.WriteLine($"  Server IP: {sqlConnection.ServerIP.HostName}");
                TestContext.WriteLine($"  Server Port: {sqlConnection.ServerPort}");
                TestContext.WriteLine($"  Datenbank: {sqlConnection.Database}");
                TestContext.WriteLine($"  Login Name: {sqlConnection.ServerLoginName}");
                TestContext.WriteLine($"  Login Passwort: {loginPassword}");

                await sqlConnection.CheckConnectionAsync(cts.Token);

                if (expectedErrorType < 0)
                {
                    // Wie erwartet ist kein Fehler aufgetreten
                    TestContext.WriteLine("Verbindung wie erwartet erfolgreich hergestellt.");
                }
                else
                {
                    // Es wurde ein Fehler erwartet, aber keiner ist aufgetreten
                    Assert.Fail("Es wurde ein Fehler erwartet, aber die Verbindung konnte erfolgreich hergestellt werden.");
                }

            }
            catch (CheckConnectionException ex)
            {
                TestContext.WriteLine($"Fehler beim Verbindungsaufbau: {ex.Message} (Fehlertyp: {ex.ExceptionType})");
                if ((int)ex.ExceptionType != expectedErrorType)
                {
                    // Der aufgetretene Fehler entspricht nicht dem erwarteten Fehler
                    Assert.Fail($"Es wurde ein Fehler vom Typ {expectedErrorType} erwartet, aber es ist ein Fehler vom Typ {ex.ExceptionType} aufgetreten.");
                }
                else
                {
                    // Der aufgetretene Fehler entspricht dem erwarteten Fehler
                    TestContext.WriteLine("Der aufgetretene Fehler entspricht dem erwarteten Fehler.");
                }
            }
        }
    }
}
