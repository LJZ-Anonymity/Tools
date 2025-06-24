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
        // 数据库路径，使用动态路径而不是硬编码路径
        private static readonly string DB_FOLDER = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Anonymity", "UsageTracker"); // 数据库文件夹路径
        private static readonly string DB_FILENAME = "UsageTracker.db"; // 数据库文件名
        private static readonly string DB_PATH = Path.Combine(DB_FOLDER, DB_FILENAME); // 数据库路径
        private static readonly string CONNECTION_STRING = $"Data Source={DB_PATH};Version=3;"; // 数据库连接字符串

        private readonly string _dbPath= DB_PATH; // 数据库路径
        private SQLiteConnection _connection; // 数据库连接

        public DatabaseHelper()
        {
            SetupSQLiteNativeLibrary();
            InitializeConnection();
        }

        // 初始化数据库连接
        private void InitializeConnection()
        {
            // 确保数据库目录存在
            if (!Directory.Exists(DB_FOLDER))
            {
                Directory.CreateDirectory(DB_FOLDER);
            }

            // 完整的数据库文件路径
            bool isNewDb = !File.Exists(_dbPath);

            // 如果数据库文件不存在，则创建一个
            if (isNewDb)
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            _connection = new SQLiteConnection(CONNECTION_STRING);
            _connection.Open();

            // 如果是新数据库，创建表结构
            if (isNewDb)
            {
                CreateDatabaseSchema();
            }
        }

        // 创建数据库表结构
        private void CreateDatabaseSchema()
        {
            SQLiteCommand cmd = new SQLiteCommand(_connection); // 创建命令对象
            try 
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SolutionUsageStats (
                        SolutionName TEXT PRIMARY KEY,
                        TotalDurationSeconds INTEGER NOT NULL,
                        DebugCount INTEGER NOT NULL DEFAULT 0
                    );
                    CREATE TABLE IF NOT EXISTS UsageSessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SolutionName TEXT NOT NULL,
                        StartTime DATETIME NOT NULL,
                        EndTime DATETIME NOT NULL,
                        DurationSeconds INTEGER NOT NULL,
                        DebugCount INTEGER NOT NULL DEFAULT 0,
                        IsClosed INTEGER NOT NULL DEFAULT 0
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS idx_SolutionStart ON UsageSessions(SolutionName, StartTime);
                    CREATE INDEX IF NOT EXISTS idx_StartTime ON UsageSessions(StartTime);
                    CREATE INDEX IF NOT EXISTS idx_SolutionName ON UsageSessions(SolutionName);
                ";
                cmd.ExecuteNonQuery(); // 执行命令
            }
            finally
            {
                cmd.Dispose(); // 释放命令对象
            }
        }

        // 释放数据库连接
        public void Dispose()
        {
            DisposeConnection();
        }

        // 释放连接
        private void DisposeConnection()
        {
            if (_connection != null)
            {
                _connection.Close(); // 关闭连接
                _connection.Dispose(); // 释放连接
                _connection = null; // 释放连接
            }
        }

        /// <summary>
        /// 记录会话
        /// </summary>
        /// <param name="session">会话</param>
        /// <param name="closeSession">是否关闭会话</param>
        public void RecordSession(UsageSession session, bool closeSession = false)
        {
            if (_connection?.State != ConnectionState.Open)
            {
                return;
            }
            InsertOrUpdateSession(session, closeSession);
            UpdateSolutionUsageStatsInternal(session.SolutionName, session.DurationSeconds);
        }

        /// <summary>
        /// 插入或更新会话
        /// </summary>
        /// <param name="session">会话</param>
        /// <param name="closeSession">是否关闭会话</param>
        private void InsertOrUpdateSession(UsageSession session, bool closeSession)
        {
            using var cmd = new SQLiteCommand(_connection);
            try
            {
                DateTime startTimeUtc = session.StartTime.ToUniversalTime();
                DateTime endTimeUtc = session.EndTime.ToUniversalTime();

                // 先保存会话
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM UsageSessions
                    WHERE SolutionName = @name AND StartTime = @start
                ";
                cmd.Parameters.AddWithValue("@name", session.SolutionName);
                cmd.Parameters.AddWithValue("@start", startTimeUtc);
                int exists = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.Parameters.Clear();

                if (exists == 0)
                {
                    // 插入新记录
                    cmd.CommandText = @"
                        INSERT INTO UsageSessions 
                        (SolutionName, StartTime, EndTime, DurationSeconds, DebugCount, IsClosed) 
                        VALUES (@name, @start, @end, @dur, @debug, @closed)";
                    cmd.Parameters.AddWithValue("@name", session.SolutionName);
                    cmd.Parameters.AddWithValue("@start", startTimeUtc);
                    cmd.Parameters.AddWithValue("@end", endTimeUtc);
                    cmd.Parameters.AddWithValue("@dur", session.DurationSeconds);
                    cmd.Parameters.AddWithValue("@debug", session.DebugCount);
                    cmd.Parameters.AddWithValue("@closed", closeSession ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    // 更新已存在记录
                    if (closeSession)
                    {
                        cmd.CommandText = @"
                            UPDATE UsageSessions
                            SET EndTime = @end, DurationSeconds = @dur + DurationSeconds, DebugCount = @debug, IsClosed = 1
                            WHERE SolutionName = @name AND StartTime = @start";
                    }
                    else
                    {
                        cmd.CommandText = @"
                            UPDATE UsageSessions
                            SET EndTime = @end, DurationSeconds = @dur + DurationSeconds, DebugCount = @debug
                            WHERE SolutionName = @name AND StartTime = @start AND IsClosed = 0";
                    }
                    cmd.Parameters.AddWithValue("@end", endTimeUtc);
                    cmd.Parameters.AddWithValue("@dur", session.DurationSeconds);
                    cmd.Parameters.AddWithValue("@debug", session.DebugCount);
                    cmd.Parameters.AddWithValue("@name", session.SolutionName);
                    cmd.Parameters.AddWithValue("@start", startTimeUtc);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        /// <summary>
        /// 获取会话
        /// </summary>
        /// <param name="fromDate">开始日期</param>
        /// <param name="toDate">结束日期</param>
        /// <returns>会话列表</returns>
        public List<UsageSession> GetSessions(DateTime? fromDate = null, DateTime? toDate = null)
        {
            return GetSessionsInternal(fromDate, toDate);
        }

        /// <summary>
        /// 获取会话
        /// </summary>
        /// <param name="fromDate">开始日期</param>
        /// <param name="toDate">结束日期</param>
        /// <returns>会话列表</returns>
        private List<UsageSession> GetSessionsInternal(DateTime? fromDate, DateTime? toDate)
        {
            var sessions = new List<UsageSession>(); // 会话列表
            if (_connection == null)
            {
                return sessions; // 如果连接为空，则返回空列表
            }

            SQLiteCommand cmd = new SQLiteCommand(_connection);
            try
            {
                cmd.CommandText = "SELECT * FROM UsageSessions WHERE 1=1"; // 查询所有会话

                if (fromDate.HasValue)
                {
                    cmd.CommandText += " AND StartTime >= @from"; // 添加开始日期条件
                    cmd.Parameters.AddWithValue("@from", fromDate.Value.ToUniversalTime());
                }

                if (toDate.HasValue)
                {
                    cmd.CommandText += " AND StartTime <= @to"; // 添加结束日期条件
                    cmd.Parameters.AddWithValue("@to", toDate.Value.ToUniversalTime());
                }

                cmd.CommandText += " ORDER BY StartTime DESC"; // 按开始时间降序排序

                // 检查表是否存在
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

                SQLiteDataReader reader = cmd.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        int id = Convert.ToInt32(reader["Id"]); // 获取ID
                        string solutionName = reader["SolutionName"].ToString(); // 获取解决方案名称
                        int durationSeconds = Convert.ToInt32(reader["DurationSeconds"]); // 获取持续时间
                        int debugCount = reader["DebugCount"] != DBNull.Value ? Convert.ToInt32(reader["DebugCount"]) : 0; // 获取调试次数

                        DateTime startTime = reader.GetDateTime(reader.GetOrdinal("StartTime")); // 获取开始时间
                        DateTime endTime = reader.GetDateTime(reader.GetOrdinal("EndTime")); // 获取结束时间

                        var session = new UsageSession
                        {
                            Id = id,
                            SolutionName = solutionName,
                            StartTime = startTime,
                            EndTime = endTime,
                            DurationSeconds = durationSeconds,
                            DebugCount = debugCount
                        };
                        sessions.Add(session);
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

        /// <summary>
        /// 更新解决方案使用统计
        /// </summary>
        /// <param name="solutionName">解决方案名称</param>
        /// <param name="durationSeconds">累计时长</param>
        public void UpdateSolutionUsageStats(string solutionName, int durationSeconds)
        {
            UpdateSolutionUsageStatsInternal(solutionName, durationSeconds);
        }

        /// <summary>
        /// 更新解决方案使用统计
        /// </summary>
        /// <param name="solutionName">解决方案名称</param>
        /// <param name="durationSeconds">累计时长</param>
        private void UpdateSolutionUsageStatsInternal(string solutionName, int durationSeconds)
        {
            if (_connection?.State != ConnectionState.Open) return;
            using var cmd = new SQLiteCommand(_connection);
            cmd.CommandText = @"
                INSERT INTO SolutionUsageStats (SolutionName, TotalDurationSeconds)
                VALUES (@name, @dur)
                ON CONFLICT(SolutionName) DO UPDATE SET TotalDurationSeconds = TotalDurationSeconds + @dur
            ";
            cmd.Parameters.AddWithValue("@name", solutionName);
            cmd.Parameters.AddWithValue("@dur", durationSeconds);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 获取所有解决方案使用统计
        /// </summary>
        /// <returns>解决方案使用统计列表</returns>
        public List<(string SolutionName, int TotalDurationSeconds)> GetAllSolutionUsageStats()
        {
            return GetAllSolutionUsageStatsInternal();
        }

        /// <summary>
        /// 获取所有解决方案使用统计
        /// </summary>
        /// <returns>解决方案使用统计列表</returns>
        private List<(string, int)> GetAllSolutionUsageStatsInternal()
        {
            var result = new List<(string, int)>(); // 解决方案名称和累计时长
            if (_connection?.State != ConnectionState.Open) return result; // 如果连接状态不是打开，则返回空列表
            using var cmd = new SQLiteCommand("SELECT SolutionName, TotalDurationSeconds FROM SolutionUsageStats ORDER BY TotalDurationSeconds DESC", _connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add((reader.GetString(0), reader.GetInt32(1))); // 将解决方案名称和累计时长添加到列表中
            }
            return result; // 返回解决方案累计时长列表
        }

        /// <summary>
        /// 获取所有解决方案使用总时长
        /// </summary>
        /// <returns>所有解决方案使用总时长</returns>
        public int GetTotalUsageSeconds()
        {
            return GetTotalUsageSecondsInternal();
        }

        /// <summary>
        /// 获取所有解决方案使用总时长
        /// </summary>
        /// <returns>所有解决方案使用总时长</returns>
        private int GetTotalUsageSecondsInternal()
        {
            if (_connection?.State != ConnectionState.Open) return 0; // 如果连接状态不是打开，则返回0
            using var cmd = new SQLiteCommand("SELECT SUM(TotalDurationSeconds) FROM SolutionUsageStats", _connection);
            var val = cmd.ExecuteScalar();
            return val != DBNull.Value && val != null ? Convert.ToInt32(val) : 0; // 如果值不是DBNull，则返回值，否则返回0
        }

        /// <summary>
        /// 增加调试次数
        /// </summary>
        /// <param name="solutionName">解决方案名称</param>
        public void IncrementDebugCount(string solutionName)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseHelper] 增加调试次数: {solutionName}");
            if (_connection?.State != ConnectionState.Open) return;
            using var cmd = new SQLiteCommand(_connection);
            cmd.CommandText = @"
                INSERT INTO SolutionUsageStats (SolutionName, TotalDurationSeconds, DebugCount)
                VALUES (@name, 0, 1)
                ON CONFLICT(SolutionName) DO UPDATE SET DebugCount = DebugCount + 1
            ";
            cmd.Parameters.AddWithValue("@name", solutionName);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 获取调试次数
        /// </summary>
        /// <param name="solutionName">解决方案名称</param>
        /// <returns>调试次数</returns>
        public int GetDebugCount(string solutionName)
        {
            if (_connection?.State != ConnectionState.Open) return 0;
            using var cmd = new SQLiteCommand(_connection);
            cmd.CommandText = "SELECT DebugCount FROM SolutionUsageStats WHERE SolutionName = @name";
            cmd.Parameters.AddWithValue("@name", solutionName);
            var val = cmd.ExecuteScalar();
            return val != DBNull.Value && val != null ? Convert.ToInt32(val) : 0;
        }

        // 设置SQLite本机库
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
                            var handle = LoadLibrary(dllPath);
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