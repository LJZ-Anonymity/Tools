using System.Data.SQLite;
using System.IO;

namespace Backup.Database
{
    class FilesDatabase
    {
        // 数据库连接字符串
        private const string Backupdb = "Data Source=C:\\Users\\LENOVO\\AppData\\Roaming\\Anonymity\\Quicker\\Extensions\\Backup\\Backup.db;Pooling=true;Max Pool Size=100;Journal Mode=Wal;";
        private const string dbFolder = @"C:\Users\LENOVO\AppData\Roaming\Anonymity\Quicker\Extensions\Backup"; // 数据库文件夹路径

        public FilesDatabase()
        {
            if (!Directory.Exists(dbFolder)) // 如果数据库文件夹不存在，则创建
                Directory.CreateDirectory(dbFolder); // 创建数据库文件夹
            string dbFilePath = Path.Combine(dbFolder, "Backup.db"); // 获取数据库文件路径
            if (!File.Exists(dbFilePath)) // 如果数据库文件不存在，则创建
            {
                SQLiteConnection.CreateFile(dbFilePath); // 创建数据库文件
                InitializeDatabase(); // 初始化数据库
            }
        }

        // 初始化数据库
        private static void InitializeDatabase()
        {
            using var connection = OpenConnection(); // 打开数据库连接
            using var command = new SQLiteCommand(SQLStatements.CreateTable, connection); // 创建命令对象
            command.ExecuteNonQuery(); // 执行创建表语句
        }

        /// <summary>
        /// 添加文件数据
        /// </summary>
        /// <param name="sourcePath"> 源路径 </param>
        /// <param name="targetPath"> 目标路径 </param>
        /// <param name="fileName"> 文件名 </param>
        /// <param name="style"> 备份方式 </param>
        /// <param name="cleanTargetFloder"> 是否清空目标文件夹 </param>
        public void AddFileData(string sourcePath, string targetPath, string fileName, string style, bool cleanTargetFloder)
        {
            using var connection = OpenConnection(); // 打开数据库连接
            using var command = new SQLiteCommand(SQLStatements.InsertFileData, connection); // 创建命令对象
            command.Parameters.AddWithValue("@SourcePath", sourcePath); // 添加参数
            command.Parameters.AddWithValue("@TargetPath", targetPath); // 添加参数
            command.Parameters.AddWithValue("@FileName", fileName); // 添加参数
            command.Parameters.AddWithValue("@Style", style); // 添加参数
            command.Parameters.AddWithValue("@CleanTargetFloder", cleanTargetFloder); // 添加参数
            command.ExecuteNonQuery(); // 执行插入语句
        }

        /// <summary>
        /// 获取所有文件数据
        /// </summary>
        /// <returns> 文件数据列表 </returns>
        public List<FileData> GetAllFileData()
        {
            using var connection = OpenConnection(); // 打开数据库连接
            using var command = new SQLiteCommand(SQLStatements.SelectAllFileData, connection); // 创建命令对象
            using var reader = command.ExecuteReader(); // 执行查询并返回结果集
            var fileDataList = new List<FileData>(); // 定义文件数据列表
            while (reader.Read())
            {
                fileDataList.Add(FileDataHelper.FromReader(reader)); // 添加文件数据
            }
            return fileDataList; // 返回所有文件数据
        }

        /// <summary>
        /// 通过ID获取备份信息
        /// </summary>
        /// <param name="fileID"> 文件ID </param>
        /// <returns> 文件数据 </returns>
        public FileData GetFileData(int fileID)
        {
            using var connection = OpenConnection(); // 打开数据库连接
            using var command = new SQLiteCommand(SQLStatements.SelectFileDataById, connection); // 创建命令对象
            command.Parameters.AddWithValue("@FileID", fileID); // 添加参数
            using var reader = command.ExecuteReader(); // 执行查询并返回结果集
            while (reader.Read())
            {
                return FileDataHelper.FromReader(reader); // 从SQLiteDataReader中读取文件数据
            }
            return null; // 文件不存在
        }

        /// <summary>
        /// 更新文件数据
        /// </summary>
        /// <param name="fileID"></param>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        /// <param name="fileName"></param>
        /// <param name="style"></param>
        public void UpdateFileData(int fileID, string sourcePath, string targetPath, string fileName, string style, bool cleanTargetFloder)
        {
            using var connection = OpenConnection(); // 打开数据库连接
            using var command = new SQLiteCommand(SQLStatements.UpdateFileData, connection); // 创建命令对象
            command.Parameters.AddWithValue("@FileID", fileID); // 添加参数
            command.Parameters.AddWithValue("@SourcePath", sourcePath); // 添加参数
            command.Parameters.AddWithValue("@TargetPath", targetPath); // 添加参数
            command.Parameters.AddWithValue("@FileName", fileName); // 添加参数
            command.Parameters.AddWithValue("@Style", style); // 添加参数
            command.Parameters.AddWithValue("@CleanTargetFloder", cleanTargetFloder); // 添加参数
            command.ExecuteNonQuery(); // 执行更新语句
        }

        /// <summary>
        /// 删除文件数据
        /// </summary>
        /// <param name="fileID"> 文件ID </param>
        public void DeleteFileData(int fileID)
        {
            using var connection = OpenConnection(); // 打开数据库连接
            using var command = new SQLiteCommand(SQLStatements.DeleteFileData, connection); // 创建命令对象
            command.Parameters.AddWithValue("@FileID", fileID); // 添加参数
            command.ExecuteNonQuery(); // 执行删除语句
        }

        /// <summary>
        /// 打开数据库连接
        /// </summary>
        /// <returns> 数据库连接 </returns>
        private static SQLiteConnection OpenConnection()
        {
            var connection = new SQLiteConnection(Backupdb); // 创建数据库连接
            connection.Open(); // 打开数据库连接
            return connection; // 返回数据库连接
        }

        // SQL语句类
        public class SQLStatements
        {
            public const string CreateTable = @"
            CREATE TABLE IF NOT EXISTS [FilesData]
            (
                FileID INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName TEXT,
                SourcePath TEXT,
                TargetPath TEXT,
                Style TEXT,
                CleanTargetFloder BOOLEAN
            );"; // 创建表语句
            public const string InsertFileData = @"
            INSERT INTO [FilesData] (SourcePath, TargetPath, FileName, Style, CleanTargetFloder)
            VALUES (@SourcePath, @TargetPath, @FileName, @Style, @CleanTargetFloder);"; // 插入语句
            public const string SelectAllFileData = @"
            SELECT * FROM [FilesData]"; // 查询所有文件数据语句
            public const string SelectFileDataById = @"
            SELECT * FROM [FilesData] WHERE FileID = @FileID"; // 查询文件数据语句
            public const string UpdateFileData = @"
            UPDATE [FilesData]
            SET SourcePath = @SourcePath, TargetPath = @TargetPath, FileName = @FileName, Style = @Style, CleanTargetFloder = @CleanTargetFloder
            WHERE FileID = @FileID"; // 更新语句
            public const string DeleteFileData = @"
            DELETE FROM [FilesData] WHERE FileID = @FileID"; // 删除语句
        }

        // 文件数据类
        public class FileData
        {
            public int FileID { get; set; } // 主键
            public required string FileName { get; set; } // 文件名
            public required string SourcePath { get; set; } // 源路径
            public required string TargetPath { get; set; } // 目标路径
            public required string Style { get; set; } // 备份方式
            public bool CleanTargetFloder { get; set; } // 是否清空目标文件夹
        }

        // 文件数据帮助类
        public static class FileDataHelper
        {
            // 从SQLiteDataReader中读取文件数据
            public static FileData FromReader(SQLiteDataReader reader)
            {
                return new FileData
                {
                    FileID = reader.GetInt32(0),
                    FileName = reader.GetString(1),
                    SourcePath = reader.GetString(2),
                    TargetPath = reader.GetString(3),
                    Style = reader.GetString(4),
                    CleanTargetFloder = reader.GetBoolean(5)
                }; // 从SQLiteDataReader中读取文件数据
            }
        }
    }
}