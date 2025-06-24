统计`Visual Studio`使用时长的扩展项目

项目文件结构：

```text
UsageTracker/
├── Commands/
│   └── ShowUsageStatsCommand.cs
├── Database/
│   ├── DatabaseHelper.cs
│   └── UsageRecord.cs
├── Models/
│   └── UsageSession.cs
├── Services/
│   ├── TrackingService.cs
│   └── IdleDetector.cs
├── ToolWindows/
│   ├── UsageStatsToolWindow.cs
│   └── UsageStatsControl.xaml
├── Properties/
│   └── AssemblyInfo.cs
├── Resources/
│   ├── Resources.resx
│   └── VSPackage.resx
├── source.extension.vsixmanifest
├── UsageTracker.csproj
└── UsageTrackerPackage.cs
```

## 注意事项

- 由于Visual Studio的限制，本扩展无法统计自身（即扩展项目）的调试时长，仅能统计普通解决方案的使用和调试时间。