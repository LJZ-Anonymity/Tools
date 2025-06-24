using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
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
                if (assemblyName == "LiveCharts.Wpf" || assemblyName == "LiveCharts")
                {
                    try
                    {
                        // 获取当前程序集所在目录
                        string extensionDir = Path.GetDirectoryName(typeof(UsageStatsToolWindow).Assembly.Location);
                        string dllPath = Path.Combine(extensionDir, $"{assemblyName}.dll");
                        if (File.Exists(dllPath))
                        {
                            var assembly = Assembly.LoadFrom(dllPath);
                            return assembly;
                        }
                    }
                    catch { }
                }
                return null;
            };
        }
        
        public UsageStatsToolWindow() : base(null)
        {
            try
            {
                Caption = "使用统计"; // 设置窗口标题

                // 检查LiveCharts程序集
                var liveChartsAssembly = Assembly.Load("LiveCharts");
                var liveChartsWpfAssembly = Assembly.Load("LiveCharts.Wpf");
                Content = new UsageStatsControl(); // 创建控件
            }
            catch { }
        }

        // 初始化方法
        protected override void Initialize()
        {
            base.Initialize(); // 调用基类初始化方法
        }
    }
}