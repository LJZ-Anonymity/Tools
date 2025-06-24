using MessageBoxResult = System.Windows.MessageBoxResult;
using UserControl = System.Windows.Controls.UserControl;
using MessageBox = System.Windows.MessageBox;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.ComponentModel;
using UsageTracker.Database;
using UsageTracker.Models;
using System.Reflection;
using System.Windows;
using LiveCharts.Wpf;
using System.Linq;
using LiveCharts;
using System.IO;
using System;

namespace UsageTracker.ToolWindows
{
    public partial class UsageStatsControl : UserControl, INotifyPropertyChanged
    {
        // 实现INotifyPropertyChanged接口
        public event PropertyChangedEventHandler PropertyChanged;

        private SeriesCollection _seriesCollection;
        private List<string> _labels;

        // 使用属性通知
        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            set
            {
                if (_seriesCollection != value)
                {
                    _seriesCollection = value;
                    OnPropertyChanged();
                }
            }
        }

        // 使用属性通知
        public List<string> Labels
        {
            get => _labels;
            set
            {
                if (_labels != value)
                {
                    _labels = value;
                    OnPropertyChanged();
                }
            }
        }

        private DatabaseHelper _dbHelper; // 数据库帮助类

        // 属性变更通知方法
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        static UsageStatsControl()
        {
            // 注册程序集解析事件
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        // 处理程序集解析
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // 获取程序集名称
            var assemblyName = new AssemblyName(args.Name).Name;

            // 如果是LiveCharts相关程序集，尝试从扩展目录加载
            if (assemblyName == "LiveCharts.Wpf" || assemblyName == "LiveCharts")
            {
                try
                {
                    // 获取当前程序集所在目录
                    string extensionDir = Path.GetDirectoryName(typeof(UsageStatsControl).Assembly.Location);
                    string dllPath = Path.Combine(extensionDir, $"{assemblyName}.dll");

                    // 如果文件存在，加载它
                    if (File.Exists(dllPath))
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        return assembly;
                    }

                    // 尝试从当前目录加载
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                    dllPath = Path.Combine(currentDir, $"{assemblyName}.dll");
                    if (File.Exists(dllPath))
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        return assembly;
                    }

                    // 尝试从GAC加载
                    try
                    {
                        var assembly = Assembly.Load(assemblyName);
                        if (assembly != null)
                        {
                            return assembly;
                        }
                    }
                    catch { }
                }
                catch { }
            }
            
            return null;
        }

        // 构造函数
        public UsageStatsControl()
        {
            try
            {
                // 初始化图表数据
                _seriesCollection = new SeriesCollection(); // 初始化图表数据
                _labels = new List<string>(); // 初始化图表标签
                
                InitializeComponent();
                
                DataContext = this; // 设置数据上下文
                Loaded += OnControlLoaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化控件失败: {ex.Message}\n{ex.StackTrace}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 控件加载事件
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 使用JoinableTaskFactory来确保在UI线程上执行
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        // 检查LiveCharts程序集是否已加载
                        var liveChartsAssembly = Assembly.Load("LiveCharts");
                        var liveChartsWpfAssembly = Assembly.Load("LiveCharts.Wpf");

                        // 初始化数据库
                        _dbHelper ??= new DatabaseHelper();

                        // 设置默认日期范围
                        dpFrom.SelectedDate = DateTime.Today.AddDays(-7);
                        dpTo.SelectedDate = DateTime.Today;

                        // 加载数据
                        LoadData();
                        LoadSolutionStats();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载控件失败: {ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载控件失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 加载数据
        private void LoadData()
        {
            try
            {
                
                // 检查数据库帮助类是否已初始化
                _dbHelper ??= new DatabaseHelper();
                
                // 使用JoinableTaskFactory来确保在UI线程上执行
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        // 获取日期范围
                        var fromDate = dpFrom.SelectedDate;
                        var toDate = dpTo.SelectedDate?.AddDays(1); // 包含结束日期

                        // 加载表格数据
                        var sessions = _dbHelper.GetSessions(fromDate, toDate);

                        // 更新DataGrid
                        dgSessions.ItemsSource = null;
                        dgSessions.ItemsSource = sessions;

                        // 生成图表数据
                        if (sessions.Count > 0)
                        {
                            GenerateChartData(sessions);
                        }
                        else
                        {
                            // 清空图表数据
                            Dispatcher.InvokeAsync(() =>
                            {
                                SeriesCollection = new SeriesCollection();
                                Labels = new List<string>();
                            });
                        }

                        // 新增：每次加载数据后刷新统计
                        LoadSolutionStats();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载数据失败: {ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {                
                MessageBox.Show($"加载数据失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 生成图表数据
        /// </summary>
        /// <param name="sessions">会话记录</param>
        private void GenerateChartData(List<UsageSession> sessions)
        {
            try
            {
                var dailyUsage = sessions
                    .GroupBy(s => s.StartTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalMinutes = g.Sum(s => s.Duration.TotalMinutes)
                    })
                    .OrderBy(d => d.Date)
                    .ToList(); // 按日期分组
                // 准备图表数据
                var newSeries = new SeriesCollection();
                var newLabels = new List<string>();

                if (dailyUsage.Any())
                {
                    // 创建柱状图系列
                    var columnSeries = new ColumnSeries
                    {
                        Title = "使用时间 (分钟)",
                        Values = new ChartValues<double>(dailyUsage.Select(d => Math.Round(d.TotalMinutes, 1)))
                    };

                    newSeries.Add(columnSeries);
                    newLabels.AddRange(dailyUsage.Select(d => d.Date.ToString("MM/dd")));
                }

                // 使用Dispatcher确保在UI线程上更新
                Dispatcher.InvokeAsync(() =>
                {
                    SeriesCollection = newSeries; // 更新系列
                    Labels = newLabels; // 更新标签
                });
            }
            catch { }
        }

        // 刷新数据
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // 导出数据
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 使用JoinableTaskFactory来确保在UI线程上执行
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        
                        if (dgSessions.ItemsSource is IEnumerable<UsageSession> sessions)
                        {
                            var saveDialog = new Microsoft.Win32.SaveFileDialog
                            {
                                Filter = "CSV文件|*.csv",
                                DefaultExt = ".csv",
                                FileName = $"VS使用统计_{DateTime.Now:yyyyMMdd}.csv"
                            };

                            if (saveDialog.ShowDialog() == true)
                            {
                                ExportToCsv(sessions.ToList(), saveDialog.FileName);
                                MessageBox.Show("导出成功!", "信息",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出失败: {ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出数据到CSV文件
        /// </summary>
        /// <param name="sessions">会话记录</param>
        /// <param name="filePath">文件路径</param>
        private void ExportToCsv(List<UsageSession> sessions, string filePath)
        {
            using var writer = new StreamWriter(filePath);
            // 写入标题
            writer.WriteLine("Solution,StartTime,EndTime,DurationSeconds");

            // 写入数据
            foreach (var session in sessions)
            {
                writer.WriteLine($"\"{session.SolutionName}\"," +
                                $"\"{session.StartTime:o}\"," +
                                $"\"{session.EndTime:o}\"," +
                                $"{session.DurationSeconds}");
            }
        }

        // 重置数据
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要重置所有使用数据吗？此操作不可撤销！",
                "确认重置", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // 使用JoinableTaskFactory来确保在UI线程上执行
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        try
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            
                            _dbHelper.Dispose();
                            
                            // 使用与DatabaseHelper相同的路径
                            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            string dbFolder = Path.Combine(appData, "Anonymity", "UsageTracker");
                            string dbPath = Path.Combine(dbFolder, "UsageTracker.db");

                            if (File.Exists(dbPath))
                            {
                                File.Delete(dbPath);
                            }

                            // 重新初始化数据库
                            _dbHelper = new DatabaseHelper();
                            LoadData();
                            LoadSolutionStats();

                            MessageBox.Show("数据已重置", "信息",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"重置失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"重置失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 新增：加载解决方案统计数据
        private void LoadSolutionStats()
        {
            var stats = _dbHelper.GetAllSolutionUsageStats()
                .Select(s => new { s.SolutionName, TotalMinutes = Math.Round(s.TotalDurationSeconds / 60.0, 1) })
                .ToList();
            dgSolutionStats.ItemsSource = stats;

            int totalSeconds = _dbHelper.GetTotalUsageSeconds();
            tbTotalUsage.Text = $"所有解决方案累计使用时长：{Math.Round(totalSeconds / 60.0, 1)} 分钟";
        }
    }
}