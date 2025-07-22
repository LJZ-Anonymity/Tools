统计`Visual Studio`使用时长的扩展项目

项目文件结构：

```text
UsageTracker/
├── Commands/                  # VS扩展命令相关代码（如显示统计窗口的命令）
│   └── ShowUsageStatsCommand.cs  # 负责显示统计窗口的命令实现
├── Database/                  # 数据库操作相关代码
│   ├── DatabaseHelper.cs         # SQLite数据库操作的核心类
│   └── UsageRecord.cs            # （如有）数据库记录模型
├── Models/                    # 业务数据模型
│   └── UsageSession.cs           # 单次使用/调试会话的数据结构
├── Services/                  # 业务逻辑服务
│   └── TrackingService.cs        # 负责会话、调试次数等统计的主服务
├── ToolWindows/               # VS扩展窗口及其UI
│   ├── UsageStatsToolWindow.cs   # 统计窗口的宿主类
│   └── UsageStatsControl.xaml    # 统计窗口的UI及其后台代码
├── Properties/                # .NET程序集属性
│   └── AssemblyInfo.cs
├── Resources/                 # 资源文件（如字符串、图标等）
│   ├── Resources.resx
│   └── VSPackage.resx
├── source.extension.vsixmanifest # VSIX扩展包清单
├── UsageTracker.csproj            # 项目文件
└── UsageTrackerPackage.cs         # VS扩展主入口，负责初始化、事件绑定等
```

## 注意事项

- 由于Visual Studio的限制，本扩展无法统计自身（即扩展项目）的调试时长，仅能统计普通解决方案的使用和调试时间。