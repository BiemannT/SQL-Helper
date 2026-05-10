using Microsoft.Data.SqlClient;
using System.Security;

namespace BiemannT.SQLHelper.Test
{
    [TestClass]
    public sealed class ExecutionTest
    {
        public TestContext TestContext { get; set; }

        public static SetupSqlConnection TestSqlConn { get; set; } = new();

        [ClassInitialize]
        public static void CreateConn(TestContext testContext)
        {
            TestSqlConn.ParseServerIP("localhost", true);
            TestSqlConn.ServerPort = 1433;
            TestSqlConn.Database = "master";
            TestSqlConn.ServerLoginName = "sa";

            string loginPwd = "Sql2Admin";
            SecureString secPwd = new();
            foreach (char c in loginPwd)
            {
                secPwd.AppendChar(c);
            }
            secPwd.MakeReadOnly();
            TestSqlConn.ServerLoginPassword = secPwd;

            TestSqlConn.CheckConnection();
        }

        [TestMethod]
        public void Test_ExecNonQuery()
        {
            // Sqlcommand vorbereiten
            using SqlCommand command = new()
            {
                CommandType = System.Data.CommandType.Text,
                CommandText = "SELECT COUNT (*) FROM sys.databases;",
                CommandTimeout = 10
            };

            try
            {
                int result = TestSqlConn.ExecNonQuery(command);
                TestContext.WriteLine($"ExecNonQuery executed successfully. Result: {result}");
                Assert.AreEqual(-1, result);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception during ExecNonQuery: {ex.GetType().FullName} - {ex.Message}");
                Assert.Fail("Unexpected exception thrown during ExecNonQuery.");
            }
        }

        [TestMethod]
        public void Test_ExecScalar()
        {
            // Sqlcommand vorbereiten
            using SqlCommand command = new()
            {
                CommandType = System.Data.CommandType.Text,
                CommandText = "SELECT COUNT (*) FROM sys.databases",
                CommandTimeout = 10
            };

            try
            {
                object? result = TestSqlConn.ExecScalar(command);
                TestContext.WriteLine($"ExecScalar executed successfully. Result: {result}");
                Assert.IsGreaterThan(1, (int)result!);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception during ExecScalar: {ex.GetType().FullName} - {ex.Message}");
                Assert.Fail("Unexpected exception thrown during ExecScalar.");
            }
        }

        [TestMethod]
        public void Test_ExecReader()
        {
            // Sqlcommand vorbereiten
            using SqlCommand command = new()
            {
                CommandType = System.Data.CommandType.Text,
                CommandText = "SELECT [name] FROM sys.databases",
                CommandTimeout = 10
            };

            try
            {
                using SqlDataReader reader = TestSqlConn.ExecReader(command);
                TestContext.WriteLine("ExecReader executed successfully. Databases:");
                while (reader.Read())
                {
                    string dbName = reader.GetString(0);
                    TestContext.WriteLine($"- {dbName}");
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Caught exception during ExecReader: {ex.GetType().FullName} - {ex.Message}");
                Assert.Fail("Unexpected exception thrown during ExecReader.");
            }
        }

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public async Task Test_ExecNonQueryAsync(bool cancelTask)
        {
            CancellationTokenSource cts = new();
            if (cancelTask)
            {
                cts.CancelAfter(5000);
            }

            // Sqlcommand vorbereiten
            using SqlCommand command = new()
            {
                CommandType = System.Data.CommandType.Text,
                CommandText = "SELECT COUNT (*) FROM sys.databases; WAITFOR DELAY '00:00:10';",
                CommandTimeout = 20
            };

            if (cancelTask)
            {
                try
                {
                    int result = await TestSqlConn.ExecNonQueryAsync(command, cts.Token);
                    TestContext.WriteLine("Execution not cancelled as expected.");
                    Assert.Fail();
                }
                catch (CheckConnectionException ex) when (ex.ExceptionType == CheckConnectionExceptionType.RequestCancelled)
                {
                    TestContext.WriteLine("Execution cancelled as expected.");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unexpected exception: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    int result = await TestSqlConn.ExecNonQueryAsync(command, TestContext.CancellationToken);
                    Assert.AreEqual(-1, result);
                    TestContext.WriteLine("Execution finished as expected.");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unexpected exception: {ex.Message}");
                }
            }
        }

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public async Task Test_ExecScalarAsync(bool cancelTask)
        {
            CancellationTokenSource cts = new();
            if (cancelTask)
            {
                cts.CancelAfter(5000);
            }

            // Sqlcommand vorbereiten
            using SqlCommand command = new()
            {
                CommandType = System.Data.CommandType.Text,
                CommandText = "SELECT COUNT (*) FROM sys.databases; WAITFOR DELAY '00:00:10';",
                CommandTimeout = 20
            };

            if (cancelTask)
            {
                try
                {
                    object? result = await TestSqlConn.ExecScalarAsync(command, cts.Token);
                    TestContext.WriteLine("Execution not cancelled as expected.");
                    Assert.Fail();
                }
                catch (CheckConnectionException ex) when (ex.ExceptionType == CheckConnectionExceptionType.RequestCancelled)
                {
                    TestContext.WriteLine("Execution cancelled as expected.");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unexpected exception: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    object? result = await TestSqlConn.ExecScalarAsync(command, cts.Token);
                    Assert.IsInstanceOfType(result, typeof(int));
                    Assert.IsGreaterThan(1, (int)result);
                    TestContext.WriteLine($"Execution finished as expected. Number databases: {result}");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unexpected exception: {ex.Message}");
                }
            }
        }

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public async Task Test_ExecReaderAsync(bool cancelTask)
        {
            CancellationTokenSource cts = new();
            if (cancelTask)
            {
                cts.CancelAfter(5000);
            }

            // Sqlcommand vorbereiten
            using SqlCommand command = new()
            {
                CommandType = System.Data.CommandType.Text,
                CommandText = "SELECT [name] FROM sys.databases; WAITFOR DELAY '00:00:10';",
                CommandTimeout = 20
            };

            if (cancelTask)
            {
                try
                {
                    using SqlDataReader result = await TestSqlConn.ExecReaderAsync(command, cts.Token);
                    TestContext.WriteLine("Execution not cancelled as expected.");
                    result.Close();
                    Assert.Fail();
                }
                catch (CheckConnectionException ex) when (ex.ExceptionType == CheckConnectionExceptionType.RequestCancelled)
                {
                    TestContext.WriteLine("Execution cancelled as expected.");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unexpected exception: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    using SqlDataReader result = await TestSqlConn.ExecReaderAsync(command, TestContext.CancellationToken);
                    TestContext.WriteLine("Execution finished as expected. Databases:");
                    while (result.Read())
                    {
                        string dbName = result.GetString(0);
                        TestContext.WriteLine($"- {dbName}");
                    }
                    result.Close();
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unexpected exception: {ex.Message}");
                }
            }
        }

        [DataRow(10)]
        [DataRow(100)]
        [DataRow(1000)]
        [DataRow(10000)]
        [TestMethod]
        public async Task Test_RaceConnections(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                // Sqlcmd vorbereiten
                using SqlCommand command = new()
                {
                    CommandType = System.Data.CommandType.Text,
                    CommandText = "SELECT @@connections;",
                    CommandTimeout = 10
                };

                TestSqlConn.ExecScalarAsync(command, TestContext.CancellationToken).Wait(1000, TestContext.CancellationToken);
            }
        }

    }
}
