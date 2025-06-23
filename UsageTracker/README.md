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