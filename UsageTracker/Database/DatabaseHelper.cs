using System.Runtime.InteropServices;
using System.Collections.Generic;
using UsageTracker.Models;
using System.Data.SQLite;
using System.Reflection;
using System.Data;
using System.IO;
using System;

namespace UsageTracker.Database
{
    public class DatabaseHelper : IDisposable
    {
        // 使用动态路径而不是硬编码路径
        private static readonly string DB_FOLDER = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Anonymity", "UsageTracker");
        private static readonly string DB_FILENAME = "UsageTracker.db";
        private static readonly string DB_PATH = Path.Combine(DB_FOLDER, DB_FILENAME);
        private static readonly string CONNECTION_STRING = $"Data Source={DB_PATH};Version=3;";

        private SQLiteConnection _connection;
        private readonly string _dbPath;

        public DatabaseHelper()
        {
            try
            {
                // 设置SQLite本机库的加载路径
                SetupSQLiteNativeLibrary();

                // 确保数据库目录存在
                if (!Directory.Exists(DB_FOLDER))
                {
                    Directory.CreateDirectory(DB_FOLDER);
                }

                // 完整的数据库文件路径
                _dbPath = DB_PATH;
                bool isNewDb = !File.Exists(_dbPath);

                // 如果数据库文件不存在，则创建一个
                if (isNewDb)
                {
                    SQLiteConnection.CreateFile(_dbPath);
                }

                // 尝试连接数据库
                try
                {
                    _connection = new SQLiteConnection(CONNECTION_STRING);
                    _connection.Open();

                    // 如果是新数据库，创建表结构
                    if (isNewDb)
                    {
                        CreateDatabaseSchema();
                    }
                }
                catch { }
            }
            catch { }
        }

        // 创建数据库表结构
        private void CreateDatabaseSchema()
        {
            SQLiteCommand cmd = new SQLiteCommand(_connection); // 创建命令对象
            try 
            {
                cmd.CommandText = @"
                    CREATE TABLE UsageSessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SolutionName TEXT NOT NULL,
                        StartTime DATETIME NOT NULL,
                        EndTime DATETIME NOT NULL,
                        DurationSeconds INTEGER NOT NULL,
                        IsClosed INTEGER NOT NULL DEFAULT 0
                    );
                    CREATE UNIQUE INDEX idx_SolutionStart ON UsageSessions(SolutionName, StartTime);
                    CREATE INDEX idx_StartTime ON UsageSessions(StartTime);
                    CREATE INDEX idx_SolutionName ON UsageSessions(SolutionName);";
                cmd.ExecuteNonQuery(); // 执行命令
            }
            finally
            {
                cmd.Dispose(); // 释放命令对象
            }
        }

        /// <summary>
        /// 记录会话
        /// </summary>
        /// <param name="session">会话对象</param>
        /// <param name="closeSession">是否关闭会话</param>
        public void RecordSession(UsageSession session, bool closeSession = false)
        {
            if (_connection?.State != ConnectionState.Open)
            {
                return;
            }

            using var cmd = new SQLiteCommand(_connection);
            try
            {
                DateTime startTimeUtc = session.StartTime.ToUniversalTime();
                DateTime endTimeUtc = session.EndTime.ToUniversalTime();

                if (closeSession)
                {
                    // 关闭会话时，设置 IsClosed=1
                    cmd.CommandText = @"
                        UPDATE UsageSessions
                        SET EndTime = @end, DurationSeconds = @dur, IsClosed = 1
                        WHERE SolutionName = @name AND StartTime = @start AND IsClosed = 0";
                }
                else
                {
                    // 只更新未关闭的会话
                    cmd.CommandText = @"
                        UPDATE UsageSessions
                        SET EndTime = @end, DurationSeconds = @dur
                        WHERE SolutionName = @name AND StartTime = @start AND IsClosed = 0";
                }
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter("@end", endTimeUtc),
                    new SQLiteParameter("@dur", session.DurationSeconds),
                    new SQLiteParameter("@name", session.SolutionName),
                    new SQLiteParameter("@start", startTimeUtc)
                });

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected == 0)
                {
                    // 没有更新到，插入新记录
                    cmd.CommandText = @"
                        INSERT INTO UsageSessions 
                        (SolutionName, StartTime, EndTime, DurationSeconds, IsClosed) 
                        VALUES (@name, @start, @end, @dur, @closed)";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddRange(new[]
                    {
                        new SQLiteParameter("@name", session.SolutionName),
                        new SQLiteParameter("@start", startTimeUtc),
                        new SQLiteParameter("@end", endTimeUtc),
                        new SQLiteParameter("@dur", session.DurationSeconds),
                        new SQLiteParameter("@closed", closeSession ? 1 : 0)
                    });
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        /// <summary>
        /// 获取会话列表
        /// </summary>
        /// <param name="fromDate">开始日期</param>
        /// <param name="toDate">结束日期</param>
        /// <returns>会话列表</returns>
        public List<UsageSession> GetSessions(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var sessions = new List<UsageSession>();

            if (_connection == null)
            {
                return sessions;
            }

            SQLiteCommand cmd = new SQLiteCommand(_connection);
            try
            {
                cmd.CommandText = "SELECT * FROM UsageSessions WHERE 1=1";

                if (fromDate.HasValue)
                {
                    cmd.CommandText += " AND StartTime >= @from";
                    cmd.Parameters.AddWithValue("@from", fromDate.Value.ToUniversalTime());
                }

                if (toDate.HasValue)
                {
                    cmd.CommandText += " AND StartTime <= @to";
                    cmd.Parameters.AddWithValue("@to", toDate.Value.ToUniversalTime());
                }

                cmd.CommandText += " ORDER BY StartTime DESC";

                // 检查表是否存在
                try
                {
                    SQLiteCommand checkCmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='UsageSessions'", _connection);
                    var tableName = checkCmd.ExecuteScalar();
                    checkCmd.Dispose();

                    if (tableName == null)
                    {
                        CreateDatabaseSchema();
                    }
                    else
                    {
                        // 检查记录数
                        SQLiteCommand countCmd = new SQLiteCommand("SELECT COUNT(*) FROM UsageSessions", _connection);
                        var count = Convert.ToInt32(countCmd.ExecuteScalar());
                        countCmd.Dispose();
                    }
                }
                catch { }

                SQLiteDataReader reader = cmd.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        try
                        {
                            int id = Convert.ToInt32(reader["Id"]); // 获取ID
                            string solutionName = reader["SolutionName"].ToString(); // 获取解决方案名称
                            int durationSeconds = Convert.ToInt32(reader["DurationSeconds"]); // 获取持续时间

                            DateTime startTime = reader.GetDateTime(reader.GetOrdinal("StartTime")); // 获取开始时间
                            DateTime endTime = reader.GetDateTime(reader.GetOrdinal("EndTime")); // 获取结束时间

                            var session = new UsageSession
                            {
                                Id = id,
                                SolutionName = solutionName,
                                StartTime = startTime,
                                EndTime = endTime,
                                DurationSeconds = durationSeconds
                            };
                            sessions.Add(session);
                        }
                        catch { }
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            }
            catch { }
            finally
            {
                cmd.Dispose();
            }
            return sessions;
        }

        // 释放数据库连接
        public void Dispose()
        {
            if (_connection != null)
            {
                try
                {
                    _connection.Close(); // 关闭连接
                    _connection.Dispose(); // 释放连接
                }
                catch { }
                _connection = null; // 释放连接
            }
        }

        // 设置SQLite本机库的加载路径
        private void SetupSQLiteNativeLibrary()
        {
            try
            {
                // 获取可能的DLL位置
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string extensionDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string vsExtensionsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                    @"Microsoft\VisualStudio\17.0_b402d05e\Extensions\UsageTracker");
                // 添加环境变量PATH，帮助系统找到SQLite.Interop.dll
                string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

                // 添加所有可能的路径
                string[] possiblePaths = new string[]
                {
                    baseDir,
                    extensionDir,
                    vsExtensionsDir,
                    Path.Combine(baseDir, "x86"),
                    Path.Combine(baseDir, "x64"),
                    Path.Combine(extensionDir, "x86"),
                    Path.Combine(extensionDir, "x64"),
                    Path.Combine(vsExtensionsDir, "x86"),
                    Path.Combine(vsExtensionsDir, "x64")
                };

                foreach (string dir in possiblePaths)
                {
                    if (Directory.Exists(dir))
                    {
                        if (!path.Contains(dir))
                        {
                            path = dir + Path.PathSeparator + path;
                        }

                        // 检查SQLite.Interop.dll是否存在
                        string dllPath = Path.Combine(dir, "SQLite.Interop.dll");
                        if (File.Exists(dllPath))
                        {
                            // 尝试预加载DLL
                            try
                            {
                                var handle = LoadLibrary(dllPath);
                            }
                            catch { }
                        }
                    }
                }

                // 设置环境变量
                Environment.SetEnvironmentVariable("PATH", path);
            }
            catch { }
        }
        
        // 导入Windows API用于加载DLL
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);
    }
}