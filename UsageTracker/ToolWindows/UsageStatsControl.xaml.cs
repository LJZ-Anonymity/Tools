using MessageBoxResult = System.Windows.MessageBoxResult;
using UserControl = System.Windows.Controls.UserControl;
using MessageBox = System.Windows.MessageBox;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Windows.Controls;
using System.ComponentModel;
using UsageTracker.Database;
using UsageTracker.Models;
using System.Reflection;
using ClosedXML.Excel;
using System.Windows;
using LiveCharts.Wpf;
using System.Linq;
using LiveCharts;
using System.IO;
using System;
using System.Threading.Tasks;

namespace UsageTracker.ToolWindows
{
    public partial class UsageStatsControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged; // 属性更改事件
        private SeriesCollection _seriesCollection; // 图表数据集合
        private DatabaseHelper _dbHelper; // 数据库帮助类
        private List<string> _labels; // 图表标签

        // 使用属性通知
        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection; // 获取系列
            set
            {
                if (_seriesCollection != value)
                {
                    _seriesCollection = value; // 设置系列
                    OnPropertyChanged(); // 通知属性已更改
                }
            }
        }

        // 使用属性通知
        public List<string> Labels
        {
            get => _labels; // 获取标签
            set
            {
                if (_labels != value)
                {
                    _labels = value; // 设置标签
                    OnPropertyChanged(); // 通知属性已更改
                }
            }
        }

        /// <summary>
        /// 实现INotifyPropertyChanged接口
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); // 通知属性已更改
        }

        static UsageStatsControl()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve; // 注册程序集解析事件
        }

        /// <summary>
        /// 处理程序集解析
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="args">解析参数</param>
        /// <returns>解析的程序集</returns>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name; // 获取程序集名称
            if (assemblyName == "LiveCharts.Wpf" || assemblyName == "LiveCharts") // 如果是LiveCharts相关程序集，尝试从扩展目录加载
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
        private async void OnControlLoaded(object sender, RoutedEventArgs e)
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
                await LoadDataAsync();
                LoadSolutionStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载控件失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 加载数据
        private async Task LoadDataAsync()
        {
            try
            {
                _dbHelper ??= new DatabaseHelper(); // 检查数据库帮助类是否已初始化
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
                    _ = Dispatcher.InvokeAsync(() =>
                    {
                        SeriesCollection = new SeriesCollection();
                        Labels = new List<string>();
                    });
                }

                // 统计选定日期范围内的使用情况
                _ = Dispatcher.InvokeAsync(() =>
                {
                    if (sessions.Count > 0)
                    {
                        double totalMinutes = sessions.Sum(s => s.Duration.TotalMinutes);
                        int totalDebugs = sessions.Sum(s => s.DebugCount);
                        tbRangeUsage.Text = $"所选日期范围内累计使用时长：{(totalMinutes < 60 ? Math.Round(totalMinutes, 1) + " 分钟" : Math.Round(totalMinutes / 60.0, 1) + " 小时")}，调试次数：{totalDebugs}";
                    }
                    else
                    {
                        tbRangeUsage.Text = "所选日期范围内无使用数据";
                    }
                });
                LoadSolutionStats(); // 每次加载数据后刷新统计
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
                        TotalMinutes = g.Sum(s => s.Duration.TotalMinutes),
                        TotalDebugs = g.Sum(s => s.DebugCount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList(); // 按日期分组
                // 准备图表数据
                var newSeries = new SeriesCollection();
                var newLabels = new List<string>();

                if (dailyUsage.Any())
                {
                    // 创建使用时间柱状图系列
                    var columnSeries = new ColumnSeries
                    {
                        Title = "使用时间 (分钟)",
                        Values = new ChartValues<double>(dailyUsage.Select(d => Math.Round(d.TotalMinutes, 1)))
                    };
                    newSeries.Add(columnSeries);

                    // 创建调试次数柱状图系列
                    var debugSeries = new ColumnSeries
                    {
                        Title = "调试次数",
                        Values = new ChartValues<double>(dailyUsage.Select(d => (double)d.TotalDebugs)),
                        ScalesYAt = 1 // 使用右侧Y轴
                    };
                    newSeries.Add(debugSeries);

                    newLabels.AddRange(dailyUsage.Select(d => d.Date.ToString("MM/dd"))); // 添加日期标签
                }

                // 使用Dispatcher确保在UI线程上更新
                Dispatcher.InvokeAsync(() =>
                {
                    SeriesCollection = newSeries; // 更新系列
                    Labels = newLabels; // 更新标签

                    // 统计总时长和总调试次数
                    double totalMinutes = sessions.Sum(s => s.Duration.TotalMinutes);
                    int totalDebugs = sessions.Sum(s => s.DebugCount);
                    if (tbChartTotalUsage != null)
                    {
                        tbChartTotalUsage.Text = totalMinutes < 60
                            ? $"总使用时长：\n{Math.Round(totalMinutes, 1)} min"
                            : $"总使用时长：\n{Math.Round(totalMinutes / 60.0, 1)} h";
                    }
                    if (tbChartTotalDebugs != null)
                    {
                        tbChartTotalDebugs.Text = $"总调试次数：\n{totalDebugs} 次";
                    }
                });
            }
            catch { }
        }

        // 刷新数据
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDataAsync(); // 刷新数据
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
                                Filter = "Excel文件|*.xlsx",
                                DefaultExt = ".xlsx",
                                FileName = $"VS使用统计_{DateTime.Now:yyyyMMdd}.xlsx"
                            };

                            if (saveDialog.ShowDialog() == true)
                            {
                                ExportToExcel(sessions.ToList(), saveDialog.FileName);
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
        /// 导出数据到Excel文件
        /// </summary>
        /// <param name="sessions">会话记录</param>
        /// <param name="filePath">文件路径</param>
        private void ExportToExcel(List<UsageSession> sessions, string filePath)
        {
            using var workbook = new XLWorkbook();
            // Sheet1: 详细数据
            var worksheet = workbook.Worksheets.Add("使用统计");
            worksheet.Cell(1, 1).Value = "解决方案";
            worksheet.Cell(1, 2).Value = "开始时间";
            worksheet.Cell(1, 3).Value = "结束时间";
            worksheet.Cell(1, 4).Value = "持续时间（秒）";
            worksheet.Cell(1, 5).Value = "调试次数";
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                worksheet.Cell(i + 2, 1).Value = session.SolutionName;
                worksheet.Cell(i + 2, 2).Value = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(i + 2, 3).Value = session.EndTime.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(i + 2, 4).Value = session.DurationSeconds;
                worksheet.Cell(i + 2, 5).Value = session.DebugCount;
            }
            worksheet.Columns().AdjustToContents();

            // Sheet2: 解决方案统计
            var statSheet = workbook.Worksheets.Add("解决方案统计");
            statSheet.Cell(1, 1).Value = "解决方案";
            statSheet.Cell(1, 2).Value = "累计时长（分钟）";
            statSheet.Cell(1, 3).Value = "调试次数";
            if (dgSolutionStats.ItemsSource != null)
            {
                int row = 2;
                foreach (var item in dgSolutionStats.ItemsSource)
                {
                    var propType = item.GetType();
                    statSheet.Cell(row, 1).Value = propType.GetProperty("SolutionName")?.GetValue(item)?.ToString();
                    statSheet.Cell(row, 2).Value = propType.GetProperty("TotalMinutes")?.GetValue(item)?.ToString();
                    statSheet.Cell(row, 3).Value = propType.GetProperty("DebugCount")?.GetValue(item)?.ToString();
                    row++;
                }
            }
            statSheet.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
        }

        // 重置数据
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要删除所有使用数据吗？此操作不可撤销！",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        try
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            
                            // 删除数据库中的所有信息
                            DeleteAllData();

                            // 重新加载数据
                            await LoadDataAsync();
                            LoadSolutionStats();

                            MessageBox.Show("所有数据已删除", "信息",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"删除失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }); // 使用JoinableTaskFactory来确保在UI线程上执行
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 删除数据库中的所有信息
        private void DeleteAllData()
        {
            try
            {
                _dbHelper ??= new DatabaseHelper(); // 检查数据库帮助类是否已初始化
                _dbHelper.DeleteAllData(); // 调用DatabaseHelper的DeleteAllData方法
            }
            catch (Exception ex)
            {
                throw new Exception($"删除数据库信息失败: {ex.Message}");
            }
        }

        // 加载解决方案统计数据
        private void LoadSolutionStats()
        {
            var stats = _dbHelper.GetAllSolutionUsageStats()
                .Select(s => new {
                    s.SolutionName,
                    TotalMinutes = Math.Round(s.TotalDurationSeconds / 60.0, 1),
                    DebugCount = _dbHelper.GetDebugCount(s.SolutionName),
                    FormattedTotalDuration = s.TotalDurationSeconds < 3600
                        ? $"{Math.Round(s.TotalDurationSeconds / 60.0, 1)} min"
                        : $"{Math.Round(s.TotalDurationSeconds / 3600.0, 1)} h"
                })
                .ToList();
            dgSolutionStats.ItemsSource = stats;

            int totalSeconds = _dbHelper.GetTotalUsageSeconds();
            tbTotalUsage.Text = $"所有解决方案累计使用时长：" + (totalSeconds < 3600 ?
                $"{Math.Round(totalSeconds / 60.0, 1)} 分钟" :
                $"{Math.Round(totalSeconds / 3600.0, 1)} 小时"); // 如果使用时长小于1小时，则单位为分钟，否则单位为小时
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDataAsync(); // 自动刷新数据
        }
    }
}