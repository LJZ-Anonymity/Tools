using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System;

namespace KeilBuilder
{
    internal static class KeilBuilderGuids
    {
        public const string guidPackageString = "b5485718-51f3-41bf-bf93-26826844683d";
        public static readonly Guid guidPackage = new Guid(guidPackageString);
        public static readonly Guid guidCommandSet = new Guid("b5485718-51f3-41bf-bf93-26826844683d");
    }
    internal static class KeilBuilderCommandIds
    {
        public const int MyMenuGroup = 0x1020;
        public const int cmdidKeilBuild = 0x0100;
    }
    /// <summary>
    /// KeilBuilder VS 扩展包主类，实现 Keil 一键编译与窗口前置功能。
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(KeilBuilderGuids.guidPackageString)]
    public sealed class KeilBuilderPackage : AsyncPackage
    {
        /// <summary>
        /// 包的 GUID 字符串，唯一标识本扩展包。
        /// </summary>
        public const string PackageGuidString = "b5485718-51f3-41bf-bf93-26826844683d";

        #region Package Members

        /// <summary>
        /// 包初始化方法，在扩展被加载时调用。用于注册自定义命令。
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progress">进度回调</param>
        /// <returns>异步任务</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
            {
                var commandId = new CommandID(KeilBuilderGuids.guidCommandSet, KeilBuilderCommandIds.cmdidKeilBuild);
                var menuCommand = new MenuCommand(this.OnKeilBuildCommand, commandId);
                mcs.AddCommand(menuCommand);
            }
        }

        /// <summary>
        /// Keil 编译命令回调。查找 Keil 进程，前置窗口并触发编译。
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void OnKeilBuildCommand(object sender, EventArgs e)
        {
            string[] keilProcessNames = { "UV5", "UV4", "UV3", "UV2" }; // 支持的 Keil 主程序进程名
            Process keilProcess = null;
            foreach (var name in keilProcessNames) // 遍历查找已运行的 Keil 进程
            {
                var found = Process.GetProcessesByName(name);
                if (found.Length > 0)
                {
                    keilProcess = found[0];
                    break;
                }
            }
            if (keilProcess != null)
            {
                SetForegroundWindow(keilProcess.MainWindowHandle); // 只前置窗口，不再启动新进程
            }
            else
            {
                VsShellUtilities.ShowMessageBox(
                    this,
                    "未检测到正在运行的 Keil，请先启动 Keil 并打开工程。",
                    "KeilBuilder",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Win32 API：将指定窗口句柄置于前台。
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否成功</returns>
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion
    }
}