using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Threading;
using UsageTracker.Database;
using UsageTracker.Models;

namespace UsageTracker.Services
{
    public class TrackingService : IDisposable
    {
        private readonly DatabaseHelper _dbHelper; // 数据库助手
        private readonly Timer _autoSaveTimer; // 自动保存计时器
        private string _currentSolution; // 当前解决方案
        private DateTime _sessionStart; // 会话开始时间
        private bool _isSolutionOpen; // 是否打开解决方案

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
            try
            {
                _isSolutionOpen = true; // 设置为打开
                _sessionStart = DateTime.Now; // 重置开始时间
                _currentSolution = solutionName; // 设置当前解决方案
                Debug.WriteLine($"开始跟踪解决方案: {solutionName}, 开始时间: {_sessionStart}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartSolutionSession错误: {ex.Message}");
            }
        }

        // 结束解决方案会话
        public void EndSolutionSession()
        {
            try
            {
                Debug.WriteLine("结束解决方案会话");
                RecordCurrentSession(true); // 关闭会话，传递true
                _isSolutionOpen = false;
                _currentSolution = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EndSolutionSession错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 自动保存回调
        /// </summary>
        /// <param name="state">状态</param>
        private void AutoSaveCallback(object state)
        {
            try
            {
                if (_isSolutionOpen)
                {
                    Debug.WriteLine("自动保存计时器触发");
                    RecordCurrentSession(); // 只更新未关闭的会话
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AutoSaveCallback错误: {ex.Message}");
            }
        }

        // 记录当前会话
        private void RecordCurrentSession(bool closeSession = false)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentSolution))
                {
                    Debug.WriteLine("无法记录会话：解决方案名称为空");
                    return;
                }

                var endTime = DateTime.Now;
                var duration = endTime - _sessionStart;

                Debug.WriteLine($"记录会话: {_currentSolution}, 持续时间: {duration.TotalSeconds}秒");

                if (duration.TotalSeconds > 5) // 最小记录时长
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        try
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            
                            var session = new UsageSession
                            {
                                SolutionName = _currentSolution,
                                StartTime = _sessionStart,
                                EndTime = endTime,
                                DurationSeconds = (int)duration.TotalSeconds
                            };
                            
                            Debug.WriteLine($"保存会话记录: {session.SolutionName}, 开始: {session.StartTime}, 结束: {session.EndTime}, 持续: {session.DurationSeconds}秒");
                            
                            try
                            {
                                _dbHelper.RecordSession(session, closeSession);
                                Debug.WriteLine("会话记录已保存到数据库");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"保存会话记录到数据库时出错: {ex.Message}");
                                if (ex.InnerException != null)
                                {
                                    Debug.WriteLine($"内部错误: {ex.InnerException.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"记录会话时出错: {ex.Message}");
                        }
                    });
                }
                else
                {
                    Debug.WriteLine($"会话时间太短 ({duration.TotalSeconds}秒)，不记录");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RecordCurrentSession错误: {ex.Message}");
            }
        }

        // 释放资源
        public void Dispose()
        {
            try
            {
                Debug.WriteLine("释放TrackingService资源");
                _autoSaveTimer?.Dispose();
                RecordCurrentSession(true); // 确保最后记录
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TrackingService.Dispose错误: {ex.Message}");
            }
        }
    }
}