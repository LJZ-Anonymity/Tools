using SolutionEvents = Microsoft.VisualStudio.Shell.Events.SolutionEvents;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Events;
using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using UsageTracker.ToolWindows;
using Microsoft.VisualStudio;
using UsageTracker.Commands;
using UsageTracker.Database;
using UsageTracker.Services;
using System.Threading;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using System;
using EnvDTE;

namespace UsageTracker
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuids.guidPackageString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(UsageStatsToolWindow))]
    public sealed class UsageTrackerPackage : AsyncPackage
    {
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper(); // 数据库帮助类
        private TrackingService _trackingService; // 跟踪服务类
        private DebuggerEvents _debuggerEvents; // 调试事件类
        private IVsSolution _solutionService; // 解决方案服务类
        private bool _isInitialized = false; // 初始化标志
        private string _currentSolution; // 当前解决方案
        private DTE _dte; // DTE对象，用于访问Visual Studio的开发环境

        /// <summary>
        /// 初始化包
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress); // 基类初始化
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken); // 切换到主线程
            try
            {
                PreloadSQLiteInterop(); // 尝试预加载SQLite.Interop.dll
                CheckDatabaseFile(); // 检查数据库文件
                _trackingService = new TrackingService(_dbHelper); // 初始化跟踪服务类

                // 获取解决方案服务
                _solutionService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution; // 获取解决方案服务

                if (_solutionService != null)
                {
                    // 订阅解决方案事件
                    SolutionEvents.OnAfterOpenSolution += HandleSolutionOpened; // 订阅解决方案打开事件
                    SolutionEvents.OnAfterCloseSolution += HandleSolutionClosed; // 订阅解决方案关闭事件
                    SolutionEvents.OnBeforeCloseSolution += HandleSolutionClosing; // 订阅解决方案关闭前事件

                    // 订阅系统事件
                    SystemEvents.SessionEnding += HandleSessionEnding; // 订阅系统结束事件

                    // 初始化命令
                    await ShowUsageStatsCommand.InitializeAsync(this); // 初始化显示使用统计命令
                    
                    _isInitialized = true; // 设置初始化标志为true

                    // 检查当前是否已有打开的解决方案
                    string currentSolution = GetSolutionName();
                    if (!string.IsNullOrEmpty(currentSolution) && currentSolution != "Unknown")
                    {
                        _currentSolution = currentSolution;
                        _trackingService.StartSolutionSession(_currentSolution);
                    }

                    _dte = await GetServiceAsync(typeof(DTE)) as DTE;
                    if (_dte != null)
                    {
                        _debuggerEvents = _dte.Events.DebuggerEvents;
                        _debuggerEvents.OnEnterRunMode += OnDebugStart;
                    }
                }
            }
            catch (DllNotFoundException) // SQLite本机库缺失错误
            {
                ShowError("无法加载SQLite库", "请确保System.Data.SQLite包正确安装，并且SQLite.Interop.dll被正确复制到输出目录。");
            }
            catch (Exception ex) // 其他一般错误
            {
                ShowError("初始化错误", $"初始化过程中遇到了问题: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="title">消息标题</param>
        /// <param name="message">消息内容</param>
        private void ShowError(string title, string message)
        {
            JoinableTaskFactory.RunAsync(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error); // 显示错误消息
            });
        }

        // 处理解决方案打开事件
        private void HandleSolutionOpened(object sender, OpenSolutionEventArgs e)
        {
            if (_isInitialized) // 确保已初始化
            {
                JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await JoinableTaskFactory.SwitchToMainThreadAsync(); // 确保在UI线程上执行
                        _trackingService.EndSolutionSession(); // 先结束上一个会话（此时 _currentSolution 还是上一个）
                        _currentSolution = GetSolutionName(); // 再开始新会话
                        _trackingService.StartSolutionSession(_currentSolution); // 开始新的解决方案会话
                    }
                    catch { }
                }); // 确保在UI线程上执行
            }
        }

        // 处理解决方案关闭事件
        private void HandleSolutionClosing(object sender, EventArgs e)
        {
            if (!_isInitialized) return;
            try
            {
                // 使用JoinableTaskFactory来确保在UI线程上执行
                JoinableTaskFactory.RunAsync(async () =>
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync();
                    _trackingService.EndSolutionSession(); // 结束解决方案会话
                });
            }
            catch { }
        }

        // 处理解决方案关闭事件
        private void HandleSolutionClosed(object sender, EventArgs e)
        {
            if (_isInitialized)
            {
                _currentSolution = null; // 设置当前解决方案为空
            }
        }

        // 处理系统结束事件
        private void HandleSessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (_isInitialized) // 确保已初始化
            {
                try
                {
                    JoinableTaskFactory.RunAsync(async () =>
                    {
                        await JoinableTaskFactory.SwitchToMainThreadAsync();
                        _trackingService.EndSolutionSession(); // 结束解决方案会话
                    }); // 使用JoinableTaskFactory来确保在UI线程上执行
                }
                catch { }
            }
        }

        /// <summary>
        /// 获取解决方案名称
        /// </summary>
        /// <returns>解决方案名称</returns>
        private string GetSolutionName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_solutionService == null) return "Unknown"; // 如果解决方案服务为空，返回Unknown

            _solutionService.GetSolutionInfo(out _, out string solutionFile, out _); // 获取解决方案信息
            return solutionFile != null ? Path.GetFileNameWithoutExtension(solutionFile) : "Unknown"; // 返回解决方案名称
        }

        /// <summary>
        /// 调试事件触发
        /// </summary>
        /// <param name="reason">调试事件原因</param>
        private void OnDebugStart(dbgEventReason reason)
        {
            if (!_isInitialized) return;
            _trackingService?.RecordDebugStart(); // 记录调试开始
            _trackingService.RecordCurrentSession(); // 记录当前会话
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否释放</param>
        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread(); // 确保在UI线程上执行
            if (disposing && _isInitialized)
            {
                // 确保所有资源被释放
                _trackingService?.Dispose(); // 释放跟踪服务
                _dbHelper?.Dispose(); // 释放数据库帮助类

                // 取消事件订阅
                SolutionEvents.OnAfterOpenSolution -= HandleSolutionOpened; // 取消解决方案打开事件订阅
                SolutionEvents.OnAfterCloseSolution -= HandleSolutionClosed; // 取消解决方案关闭事件订阅
                SolutionEvents.OnBeforeCloseSolution -= HandleSolutionClosing; // 取消解决方案关闭前事件订阅
                SystemEvents.SessionEnding -= HandleSessionEnding; // 取消系统结束事件订阅

                if (_debuggerEvents != null)
                {
                    _debuggerEvents.OnEnterRunMode -= OnDebugStart; // 取消调试开始事件订阅
                }
            }
            base.Dispose(disposing); // 基类释放
        }

        // 预加载SQLite.Interop.dll
        private void PreloadSQLiteInterop()
        {
            try
            {
                // 获取可能的DLL路径
                string vsExtensionsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                    @"Microsoft\VisualStudio\17.0_b402d05e\Extensions\UsageTracker");
                string extensionDir = Path.GetDirectoryName(typeof(UsageTrackerPackage).Assembly.Location);
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string[] possiblePaths = new string[]
                {
                    vsExtensionsDir,
                    extensionDir,
                    baseDir
                }; // 定义可能的路径

                // 检查每个路径
                foreach (string dir in possiblePaths)
                {
                    if (Directory.Exists(dir))
                    {
                        string dllPath = Path.Combine(dir, "SQLite.Interop.dll"); // 拼接DLL路径
                        if (File.Exists(dllPath))
                        {
                            var handle = LoadLibrary(dllPath); // 加载DLL
                            if (handle != IntPtr.Zero)
                            {
                                return; // 成功加载，退出
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        // 导入Windows API用于加载DLL
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        // 检查数据库文件
        private void CheckDatabaseFile()
        {
            try
            {
                // 数据库路径
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dbFolder = Path.Combine(appData, "Anonymity", "UsageTracker");
                string dbPath = Path.Combine(dbFolder, "UsageTracker.db");

                // 检查目录是否存在
                if (!Directory.Exists(dbFolder))
                {
                    Directory.CreateDirectory(dbFolder);
                }

                // 检查文件是否存在
                if (File.Exists(dbPath))
                {
                    // 检查文件权限
                    using var fs = File.Open(dbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
            }
            catch { }
        }
    }
}