using Microsoft.VisualStudio.Shell;
using UsageTracker.Database;
using UsageTracker.Models;
using System.Threading;
using System;

namespace UsageTracker.Services
{
    public class TrackingService : IDisposable
    {
        private DateTime _sessionRecordTime = DateTime.Now; // 会话记录时间
        private readonly DatabaseHelper _dbHelper; // 数据库助手
        private readonly Timer _autoSaveTimer; // 自动保存计时器
        private string _currentSolution; // 当前解决方案
        private DateTime _sessionStart; // 会话开始时间
        private bool _isSolutionOpen; // 是否打开解决方案
        private int _sessionDebugCount = 0; // 当前会话调试次数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbHelper">数据库助手</param>
        public TrackingService(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
            _autoSaveTimer = new Timer(AutoSaveCallback, null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// 开始解决方案会话
        /// </summary>
        /// <param name="solutionName">解决方案名称</param>
        public void StartSolutionSession(string solutionName)
        {
            _isSolutionOpen = true; // 设置为打开
            _sessionStart = DateTime.Now; // 设置会话开始时间
            _currentSolution = solutionName; // 设置当前解决方案
            _sessionDebugCount = 0; // 重置调试次数
            System.Diagnostics.Debug.WriteLine($"[TrackingService] 会话开始: {solutionName}");
            _dbHelper.UpdateSolutionUsageStats(solutionName, 0); // 确保有行
        }

        // 结束解决方案会话
        public void EndSolutionSession()
        {
            RecordCurrentSession(true); // 关闭会话，传递true
            _isSolutionOpen = false;
            _currentSolution = null;
            _sessionDebugCount = 0; // 结束时重置
        }

        /// <summary>
        /// 自动保存回调
        /// </summary>
        /// <param name="state">状态</param>
        private void AutoSaveCallback(object state)
        {
            if (_isSolutionOpen)
            {
                RecordCurrentSession(); // 只更新未关闭的会话
            }
        }

        // 记录当前会话
        private void RecordCurrentSession(bool closeSession = false)
        {
            if (string.IsNullOrEmpty(_currentSolution))
            {
                return;
            }

            var endTime = DateTime.Now;
            var duration = endTime - _sessionRecordTime;
            _sessionRecordTime = DateTime.Now; // 重置开始时间

            if (duration.TotalSeconds > 5) // 最小记录时长
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var session = new UsageSession
                    {
                        SolutionName = _currentSolution,
                        StartTime = _sessionStart,
                        EndTime = endTime,
                        DurationSeconds = (int)duration.TotalSeconds,
                        DebugCount = _sessionDebugCount // 写入调试次数
                    };
                    _dbHelper.RecordSession(session, closeSession); // 记录会话
                }); // 在主线程中执行
            }
        }

        // 记录调试启动
        public void RecordDebugStart()
        {
            if (!string.IsNullOrEmpty(_currentSolution))
            {
                _sessionDebugCount++; // 当前会话调试次数+1
                System.Diagnostics.Debug.WriteLine($"[TrackingService] 记录调试启动: {_currentSolution}, 当前会话调试次数: {_sessionDebugCount}");
                _dbHelper.IncrementDebugCount(_currentSolution);
            }
        }

        // 释放资源
        public void Dispose()
        {
            _autoSaveTimer?.Dispose();
            RecordCurrentSession(true); // 确保最后记录
        }
    }
}