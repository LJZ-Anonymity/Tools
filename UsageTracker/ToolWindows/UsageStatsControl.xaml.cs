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
            System.Diagnostics.Debug.WriteLine($"属性变更通知: {propertyName}");
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
            
            System.Diagnostics.Debug.WriteLine($"尝试解析程序集: {args.Name}, 名称: {assemblyName}");
            
            // 如果是LiveCharts相关程序集，尝试从扩展目录加载
            if (assemblyName == "LiveCharts.Wpf" || assemblyName == "LiveCharts")
            {
                try
                {
                    // 获取当前程序集所在目录
                    string extensionDir = Path.GetDirectoryName(typeof(UsageStatsControl).Assembly.Location);
                    string dllPath = Path.Combine(extensionDir, $"{assemblyName}.dll");
                    
                    System.Diagnostics.Debug.WriteLine($"尝试从扩展目录加载: {dllPath}");
                    
                    // 如果文件存在，加载它
                    if (File.Exists(dllPath))
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        System.Diagnostics.Debug.WriteLine($"从扩展目录成功加载: {assemblyName}");
                        return assembly;
                    }
                    
                    // 尝试从当前目录加载
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                    dllPath = Path.Combine(currentDir, $"{assemblyName}.dll");
                    
                    System.Diagnostics.Debug.WriteLine($"尝试从当前目录加载: {dllPath}");
                    
                    if (File.Exists(dllPath))
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        System.Diagnostics.Debug.WriteLine($"从当前目录成功加载: {assemblyName}");
                        return assembly;
                    }
                    
                    // 尝试从GAC加载
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"尝试从GAC加载: {assemblyName}");
                        var assembly = Assembly.Load(assemblyName);
                        if (assembly != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"从GAC成功加载: {assemblyName}");
                            return assembly;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"从GAC加载失败: {ex.Message}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"无法找到程序集: {assemblyName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载程序集 {assemblyName} 失败: {ex.Message}");
                }
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
                
                System.Diagnostics.Debug.WriteLine("UsageStatsControl构造函数完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UsageStatsControl初始化错误: {ex.Message}");
                MessageBox.Show($"初始化控件失败: {ex.Message}\n{ex.StackTrace}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 控件加载事件
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("控件加载事件触发");
                
                // 使用JoinableTaskFactory来确保在UI线程上执行
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        System.Diagnostics.Debug.WriteLine("已切换到UI线程");
                        
                        // 检查LiveCharts程序集是否已加载
                        try
                        {
                            var liveChartsAssembly = Assembly.Load("LiveCharts");
                            var liveChartsWpfAssembly = Assembly.Load("LiveCharts.Wpf");
                            System.Diagnostics.Debug.WriteLine($"LiveCharts程序集已加载: {liveChartsAssembly.GetName().Version}");
                            System.Diagnostics.Debug.WriteLine($"LiveCharts.Wpf程序集已加载: {liveChartsWpfAssembly.GetName().Version}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"LiveCharts程序集加载失败: {ex.Message}");
                        }
                        
                        // 初始化数据库
                        if (_dbHelper == null)
                        {
                            System.Diagnostics.Debug.WriteLine("初始化数据库");
                            _dbHelper = new DatabaseHelper();
                        }

                        // 设置默认日期范围
                        dpFrom.SelectedDate = DateTime.Today.AddDays(-7);
                        dpTo.SelectedDate = DateTime.Today;
                        System.Diagnostics.Debug.WriteLine($"设置日期范围: {dpFrom.SelectedDate:yyyy-MM-dd} 至 {dpTo.SelectedDate:yyyy-MM-dd}");

                        // 加载数据
                        System.Diagnostics.Debug.WriteLine("开始加载初始数据");
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"OnControlLoaded错误 (UI线程): {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"内部错误: {ex.InnerException.Message}");
                        }
                        
                        MessageBox.Show($"加载控件失败: {ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnControlLoaded错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
                
                MessageBox.Show($"加载控件失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 加载数据
        private void LoadData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始加载数据...");
                
                // 检查数据库帮助类是否已初始化
                if (_dbHelper == null)
                {
                    System.Diagnostics.Debug.WriteLine("数据库帮助类未初始化，尝试创建");
                    _dbHelper = new DatabaseHelper();
                }
                
                // 使用JoinableTaskFactory来确保在UI线程上执行
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        
                        System.Diagnostics.Debug.WriteLine("已切换到UI线程");
                        
                        // 获取日期范围
                        var fromDate = dpFrom.SelectedDate;
                        var toDate = dpTo.SelectedDate?.AddDays(1); // 包含结束日期
                        
                        System.Diagnostics.Debug.WriteLine($"查询日期范围: {fromDate:yyyy-MM-dd} 至 {toDate:yyyy-MM-dd}");
                        
                        // 加载表格数据
                        var sessions = _dbHelper.GetSessions(fromDate, toDate);

                        System.Diagnostics.Debug.WriteLine($"加载了 {sessions.Count} 条会话记录");
                        
                        // 更新DataGrid
                        dgSessions.ItemsSource = null;
                        dgSessions.ItemsSource = sessions;
                        
                        System.Diagnostics.Debug.WriteLine("DataGrid数据源已更新");

                        // 生成图表数据
                        if (sessions.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("开始生成图表数据");
                            GenerateChartData(sessions);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("没有数据，清空图表");
                            // 清空图表数据
                            Dispatcher.InvokeAsync(() =>
                            {
                                SeriesCollection = new SeriesCollection();
                                Labels = new List<string>();
                                System.Diagnostics.Debug.WriteLine("图表数据已清空");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadData错误 (UI线程): {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"内部错误: {ex.InnerException.Message}");
                        }
                        
                        MessageBox.Show($"加载数据失败: {ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadData错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
                
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
                System.Diagnostics.Debug.WriteLine($"开始生成图表数据，会话数: {sessions.Count}");
                var dailyUsage = sessions
                    .GroupBy(s => s.StartTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalMinutes = g.Sum(s => s.Duration.TotalMinutes)
                    })
                    .OrderBy(d => d.Date)
                    .ToList(); // 按日期分组

                System.Diagnostics.Debug.WriteLine($"生成了 {dailyUsage.Count} 天的图表数据");
                
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
                    
                    foreach (var item in dailyUsage)
                    {
                        System.Diagnostics.Debug.WriteLine($"图表数据: {item.Date:MM/dd} - {Math.Round(item.TotalMinutes, 1)}分钟");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("没有图表数据可显示");
                }
                
                // 使用Dispatcher确保在UI线程上更新
                Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("开始更新UI上的图表数据");

                        SeriesCollection = newSeries; // 更新系列
                        Labels = newLabels; // 更新标签

                        System.Diagnostics.Debug.WriteLine($"图表数据已更新，系列数: {SeriesCollection.Count}, 标签数: {Labels.Count}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"更新图表数据时出错: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"内部错误: {ex.InnerException.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GenerateChartData错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
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
                        System.Diagnostics.Debug.WriteLine($"BtnExport_Click错误 (UI线程): {ex.Message}");
                        MessageBox.Show($"导出失败: {ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BtnExport_Click错误: {ex.Message}");
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

                            System.Diagnostics.Debug.WriteLine($"尝试删除数据库文件: {dbPath}");
                            
                            if (File.Exists(dbPath))
                            {
                                File.Delete(dbPath);
                                System.Diagnostics.Debug.WriteLine("数据库文件已删除");
                            }

                            // 重新初始化数据库
                            _dbHelper = new DatabaseHelper();
                            LoadData();

                            MessageBox.Show("数据已重置", "信息",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"BtnReset_Click错误 (UI线程): {ex.Message}");
                            MessageBox.Show($"重置失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BtnReset_Click错误: {ex.Message}");
                    MessageBox.Show($"重置失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}