using System.Diagnostics;
using System.Security;

namespace BiemannT.SQLHelper.Test
{
    [TestClass]
    public sealed class SqlConnectionTest
    {
        public TestContext TestContext { get; set; }

        // Ohne DNS-Prüfung
        [DataRow("", false, true, false)]
        [DataRow(" ", false, true, false)]
        [DataRow("nonexistent.domain.test", false, false, true)]
        [DataRow("localhost", false, false, true)]
        [DataRow("8.8.8.8", false, false, false)]
        [DataRow("1.2.3.4", false, false, false)]
        // Mit DNS-Prüfung
        [DataRow("", true, true, false)]
        [DataRow(" ", true, true, false)]
        [DataRow("nonexistent.domain.test", true, false, true)]
        [DataRow("localhost", true, false, false)]
        [DataRow("8.8.8.8", true, false, false)]
        [DataRow("1.2.3.4", true, false, true)]
        [TestMethod]
        public void Test_ParseSQLServerIP(string hostName, bool checkDNS, bool argumentExceptionExpected, bool invalidOperationExceptionExpected)
        {
            SetupSqlConnection sqlconnection = new();

            TestContext.WriteLine($"Testing with HostName: '{hostName}', CheckDNS: {checkDNS}");

            try
            {
                sqlconnection.ParseServerIP(hostName, checkDNS);

                TestContext.WriteLine($"Resolved IP-Address: {sqlconnection.ServerIP.AddressList[0]}");
                TestContext.WriteLine($"Host Name: {sqlconnection.ServerIP.HostName}");

                // TryParseSQLServerIP mit testen
                Assert.IsTrue(sqlconnection.TryParseServerIP(hostName, checkDNS));
                TestContext.WriteLine("TryParseSQLServerIP returned true as expected.");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception: {ex.GetType().FullName} - {ex.Message}");

                // TryParseSQLServerIP mit testen
                Assert.IsFalse(sqlconnection.TryParseServerIP(hostName, checkDNS));
                TestContext.WriteLine("TryParseSQLServerIP returned false as expected.");

                if (argumentExceptionExpected)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentException));
                    return;
                }

                if (invalidOperationExceptionExpected)
                {
                    Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                    return;
                }

                Assert.Fail("Unexpected exception type caught.");
            }
        }

        [DataRow(-10)]
        [DataRow(70000)]
        [DataRow(1433)]
        [TestMethod]
        public void Test_SQLServerPort(int port)
        {
            SetupSqlConnection sqlconnection = new();

            TestContext.WriteLine($"Testing port number: '{port}'");

            try
            {
                sqlconnection.ServerPort = port;

                TestContext.WriteLine($"Port number {port} valid.");

                Assert.AreEqual(port, sqlconnection.ServerPort);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception: {ex.GetType().FullName} - {ex.Message}");

                Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
            }
        }

        [DataRow("")]
        [DataRow(" ")]
        [DataRow("validLoginName")]
        [TestMethod]
        public void Test_LoginName(string loginName)
        {
            SetupSqlConnection sqlconnection = new();

            TestContext.WriteLine($"Testing login name: '{loginName}'");

            try
            {
                sqlconnection.ServerLoginName = loginName;

                TestContext.WriteLine($"Login name '{loginName}' valid.");

                Assert.AreEqual(loginName, sqlconnection.ServerLoginName);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception: {ex.GetType().FullName} - {ex.Message}");

                Assert.IsInstanceOfType(ex, typeof(ArgumentException));
            }
        }

        [DataRow("", false)]
        [DataRow(" ", false)]
        [DataRow("validPassword", false)]
        [DataRow("", true)]
        [DataRow(" ", true)]
        [DataRow("validPassword", true)]
        [TestMethod]
        public void Test_LoginPassword(string password, bool makeReadOnly)
        {
            SetupSqlConnection sqlconnection = new();

            TestContext.WriteLine($"Testing password: '{password}'");

            try
            {
                SecureString securePassword = new();
                foreach (char c in password)
                {
                    securePassword.AppendChar(c);
                }

                if (makeReadOnly)
                {
                    securePassword.MakeReadOnly();
                }

                sqlconnection.ServerLoginPassword = securePassword;

                TestContext.WriteLine($"Password set successfully.");

                Assert.AreEqual(securePassword.Length, sqlconnection.ServerLoginPassword.Length);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception: {ex.GetType().FullName} - {ex.Message}");

                // Bei einem nicht schreibgeschützten SecureString wird eine ArgumentException erwartet
                if (!makeReadOnly)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentException));
                }

                // Ansonsten wird eine ArgumentOutOfRangeException erwartet
                else
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }
            }
        }

        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(1000)]
        [DataRow(300000)]
        [DataRow(300001)]
        [TestMethod]
        public void Test_ConnectTimeout(int milliseconds)
        {
            SetupSqlConnection sqlconnection = new();

            TestContext.WriteLine($"Testing ConnectTimeout: '{milliseconds}' milliseconds");

            try
            {
                sqlconnection.ConnectTimeout = TimeSpan.FromMilliseconds(milliseconds);

                TestContext.WriteLine($"ConnectTimeout set to '{milliseconds}' milliseconds successfully.");

                Assert.AreEqual(TimeSpan.FromMilliseconds(milliseconds), sqlconnection.ConnectTimeout);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception: {ex.GetType().FullName} - {ex.Message}");

                Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
            }
        }

        [DataRow("")]
        [DataRow(" ")]
        [DataRow("ValidDatabaseName")]
        [TestMethod]
        public void Test_Database(string databaseName)
        {
            SetupSqlConnection sqlconnection = new();

            TestContext.WriteLine($"Testing Database name: '{databaseName}'");

            try
            {
                sqlconnection.Database = databaseName;

                TestContext.WriteLine($"Database name '{databaseName}' set successfully.");

                Assert.AreEqual(databaseName, sqlconnection.Database);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception: {ex.GetType().FullName} - {ex.Message}");

                Assert.IsInstanceOfType(ex, typeof(ArgumentException));
            }
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MSTEST0049:Fluss von TestContext.CancellationToken zu asynchronen Vorgängen", Justification = "<Ausstehend>")]
        public void Test_ThreadSafety()
        {
            // Verschiedene Tasks sollen die Eigenschaften LoginName und Port tauschen
            SetupSqlConnection sqlConnection = new();
            Task[] tasks = new Task[10];

            for (int i = 0; i < 10; i++)
            {
                int taskIndex = i;
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    // Random Delay
                    Random randDelay = new(i);
                    // Werte setzen
                    Task.Delay(randDelay.Next(10, 20)).Wait(); // Kurze Verzögerung einfügen, um Thread-Wechsel zu erzwingen
                    for (int j = 0; j < 200 * i; j++)
                    {
                        sqlConnection.ServerLoginName = $"User{i}{j}";
                    }

                    //Task.Delay(randDelay.Next(10, 20)).Wait(); // Kurze Verzögerung einfügen, um Thread-Wechsel zu erzwingen
                    for (int j = 0; j < 300 * i; j++)
                    {
                        sqlConnection.ServerPort = 2 * i * j;
                    }

                    // Werte lesen und überprüfen
                    Debug.WriteLine($"Task {taskIndex}: Login {sqlConnection.ServerLoginName} / Port {sqlConnection.ServerPort}");
                });
            }

            Task.Factory.ContinueWhenAll(tasks, completedTasks =>
            {
                Debug.WriteLine("All tasks completed.");
            }).Wait();
        }

        [DataRow("ConnectTimeout", (int)60)]
        [DataRow("Database", "TestDB")]
        [DataRow("ServerInstance", "TestInstance")]
        [DataRow("ServerIP", "127.0.0.1")]
        [DataRow("ServerLoginName", "TestUser")]
        [DataRow("ServerLoginPassword", "TestPassword")]
        [DataRow("ServerPort", 1500)]
        [TestMethod]
        public void Test_PropertyChangedNotification(string propertyName, object value)
        {
            TestContext.WriteLine($"Testing PropertyChanged notification for property: '{propertyName}' with value: '{value}'");

            SetupSqlConnection sqlConnection = new();

            Task<string?> taskChange = Task.Run(() =>
            {
                string? changedProperty = null;

                sqlConnection.PropertyChanged += (sender, e) =>
                {
                    changedProperty = e.PropertyName;
                };

                switch (propertyName)
                {
                    case nameof(sqlConnection.ConnectTimeout):
                        sqlConnection.ConnectTimeout = TimeSpan.FromSeconds((int)value);
                        break;

                    case nameof(sqlConnection.Database):
                        sqlConnection.Database = (string)value;
                        break;

                    case nameof(sqlConnection.ServerInstance):
                        sqlConnection.ServerInstance = (string)value;
                        break;

                    case nameof(sqlConnection.ServerIP):
                        sqlConnection.ParseServerIP((string)value);
                        break;

                    case nameof(sqlConnection.ServerLoginName):
                        sqlConnection.ServerLoginName = (string)value;
                        break;

                    case nameof(sqlConnection.ServerLoginPassword):
                        SecureString securePassword = new();
                        foreach (char c in (string)value)
                        {
                            securePassword.AppendChar(c);
                        }
                        securePassword.MakeReadOnly();
                        sqlConnection.ServerLoginPassword = securePassword;
                        break;

                    case nameof(sqlConnection.ServerPort):
                        sqlConnection.ServerPort = (int)value;
                        break;

                    default:
                        break;
                }

                return changedProperty;
            });

            string? result = taskChange.Result;
            Assert.AreEqual(propertyName, result);
        }
    }
}