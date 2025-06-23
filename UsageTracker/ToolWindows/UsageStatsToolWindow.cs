using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System;

namespace UsageTracker.ToolWindows
{
    [Guid("c8f4dcf1-0d8c-4b8c-a1c3-8d5f4e9f4d7b")] // 定义窗口的GUID
    public class UsageStatsToolWindow : ToolWindowPane
    {
        static UsageStatsToolWindow()
        {
            // 确保LiveCharts程序集可以被加载
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblyName = new AssemblyName(args.Name).Name;
                Debug.WriteLine($"工具窗口尝试解析程序集: {args.Name}, 名称: {assemblyName}");
                if (assemblyName == "LiveCharts.Wpf" || assemblyName == "LiveCharts")
                {
                    try
                    {
                        // 获取当前程序集所在目录
                        string extensionDir = Path.GetDirectoryName(typeof(UsageStatsToolWindow).Assembly.Location);
                        string dllPath = Path.Combine(extensionDir, $"{assemblyName}.dll");
                        
                        Debug.WriteLine($"工具窗口尝试从扩展目录加载: {dllPath}");
                        
                        if (File.Exists(dllPath))
                        {
                            var assembly = Assembly.LoadFrom(dllPath);
                            Debug.WriteLine($"工具窗口从扩展目录成功加载: {assemblyName}");
                            return assembly;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"工具窗口加载程序集失败: {ex.Message}");
                    }
                }
                
                return null;
            };
        }
        
        public UsageStatsToolWindow() : base(null)
        {
            try
            {
                Debug.WriteLine("开始创建UsageStatsToolWindow");
                
                Caption = "使用统计"; // 设置窗口标题
                
                // 检查LiveCharts程序集
                try
                {
                    var liveChartsAssembly = Assembly.Load("LiveCharts");
                    var liveChartsWpfAssembly = Assembly.Load("LiveCharts.Wpf");
                    Debug.WriteLine($"工具窗口: LiveCharts程序集已加载: {liveChartsAssembly.GetName().Version}");
                    Debug.WriteLine($"工具窗口: LiveCharts.Wpf程序集已加载: {liveChartsWpfAssembly.GetName().Version}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"工具窗口: LiveCharts程序集加载失败: {ex.Message}");
                }
                
                Content = new UsageStatsControl(); // 创建控件
                Debug.WriteLine("UsageStatsToolWindow已创建"); // 输出创建信息
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建UsageStatsToolWindow时出错: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }

        // 初始化方法
        protected override void Initialize()
        {
            base.Initialize(); // 调用基类初始化方法
            Debug.WriteLine("UsageStatsToolWindow已初始化"); // 输出初始化信息
        }
    }
}