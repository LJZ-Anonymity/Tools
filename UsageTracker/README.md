# UsageTracker - Visual Studio 使用统计扩展

> 统计和分析 Visual Studio 使用时长、调试次数的扩展工具，支持数据可视化与一键导出 Excel

## 项目简介

UsageTracker 是一个专为 Visual Studio 开发人员设计的统计分析扩展，能够自动跟踪和记录您在 Visual Studio 中的使用情况。通过直观的数据可视化界面，您可以了解自己的开发习惯、项目使用时长和调试频率，帮助优化开发效率。

## 主要功能

- **自动使用时长统计**：自动记录解决方案打开和关闭时间
- **调试次数统计**：统计每个项目的调试启动次数
- **数据可视化**：使用图表展示使用趋势和统计信息
- **Excel 导出**：支持一键导出统计数据到 Excel 文件
- **多项目支持**：同时跟踪多个解决方案的使用情况
- **实时监控**：后台自动运行，无需手动干预

## 安装方法

1. 下载 `.vsix` 扩展文件
2. 双击安装或通过 Visual Studio 的扩展管理器安装
3. 重启 Visual Studio

## 使用方法

1. **自动统计**：安装后扩展会自动开始统计使用情况
2. **查看统计**：
   - 点击 **工具** → **显示使用统计** 菜单项
   - 或使用快捷键打开统计窗口
3. **数据导出**：在统计窗口中点击导出按钮，将数据保存为 Excel 文件

## 统计内容

- **解决方案使用时长**：每个解决方案的总使用时间
- **调试次数**：每个项目的调试启动次数
- **使用趋势**：按日期统计的使用时长变化
- **项目排名**：按使用时长排序的项目列表

## 适用场景

- 个人开发效率分析
- 项目时间管理
- 开发习惯优化
- 团队开发时间统计

## 项目文件结构

```text
UsageTracker/
├── Commands/                  # VS扩展命令相关代码
│   └── ShowUsageStatsCommand.cs  # 负责显示统计窗口的命令实现
├── Database/                  # 数据库操作相关代码
│   └── DatabaseHelper.cs         # SQLite数据库操作的核心类
├── Models/                    # 业务数据模型
│   └── UsageSession.cs           # 单次使用/调试会话的数据结构
├── Services/                  # 业务逻辑服务
│   └── TrackingService.cs        # 负责会话、调试次数等统计的主服务
├── ToolWindows/               # VS扩展窗口及其UI
│   ├── UsageStatsToolWindow.cs   # 统计窗口的宿主类
│   ├── UsageStatsControl.xaml    # 统计窗口的UI界面
│   └── UsageStatsControl.xaml.cs # 统计窗口的后台代码
├── Properties/                # .NET程序集属性
│   └── AssemblyInfo.cs
├── Resources/                 # 资源文件
│   ├── Resources.resx
│   └── VSPackage.resx
├── source.extension.vsixmanifest # VSIX扩展包清单
├── UsageTracker.csproj            # 项目文件
├── VSCommandTable.vsct            # 命令表文件，定义菜单和快捷键
├── VSCommandTable.cs              # 命令表代码生成文件
└── UsageTrackerPackage.cs         # VS扩展主入口，负责初始化、事件绑定等
```

## 技术实现

- **开发语言**：C#
- **目标框架**：.NET Framework 4.5+
- **Visual Studio 版本**：支持 VS 2022 (17.0+)
- **数据库**：SQLite 本地数据库
- **图表库**：LiveCharts.Wpf 数据可视化
- **扩展类型**：VSIX Package

## 开发环境

- Visual Studio 2022
- .NET Framework 4.5 或更高版本
- Microsoft.VSSDK.BuildTools
- System.Data.SQLite
- LiveCharts.Wpf

## 数据存储

- **数据库文件**：`UsageTracker.db` (SQLite)
- **存储位置**：用户文档目录下的扩展专用文件夹
- **数据备份**：支持导出 Excel 格式备份

## 注意事项

- 由于 Visual Studio 的限制，本扩展无法统计自身（即扩展项目）的调试时长，仅能统计普通解决方案的使用和调试时间
- 统计数据存储在本地，不会上传到任何服务器
- 建议定期导出数据备份，以防数据丢失

## 许可证

本项目采用 MIT 许可证，详见 [LICENSE.txt](LICENSE.txt) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个扩展！