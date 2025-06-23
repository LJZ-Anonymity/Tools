using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using UsageTracker.ToolWindows;
using System;

namespace UsageTracker.Commands
{
    internal sealed class ShowUsageStatsCommand
    {
        /// <summary>
        /// 初始化命令
        /// </summary>
        /// <param name="package">AsyncPackage实例</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(); // 确保在UI线程上执行

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService; // 获取命令服务
            if (commandService == null) return; // 如果命令服务为空，直接返回

            var commandId = new CommandID(PackageGuids.guidCommandSet, CommandIds.cmdidShowUsageStats); // 创建命令ID
            var menuCommand = new MenuCommand((sender, e) => Execute(package), commandId); // 创建菜单命令
            commandService.AddCommand(menuCommand); // 添加命令
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="package">AsyncPackage实例</param>
        private static void Execute(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread(); // 确保在UI线程上执行
            package.JoinableTaskFactory.Run(async () =>
            {
                ToolWindowPane window = await package.ShowToolWindowAsync(
                    typeof(UsageStatsToolWindow),
                    0,
                    create: true,
                    cancellationToken: package.DisposalToken); // 使用DisposalToken

                if (window?.Frame == null)
                {
                    throw new NotSupportedException("无法创建工具窗口");
                }
            });
        }
    }
}