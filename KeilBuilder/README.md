# KeilBuilder - Visual Studio 扩展

> 代替Keil进行代码编译的Visual Studio插件。快捷键Alt+K将Keil实例置于前台进行Build

## 项目简介

KeilBuilder 是一个专为嵌入式开发人员设计的 Visual Studio 扩展，旨在简化 Visual Studio 与 Keil uVision 之间的工作流程。通过这个扩展，您可以在 Visual Studio 中快速访问正在运行的 Keil 实例，无需手动切换窗口。

## 主要功能

- **快速访问 Keil 实例**：在 Visual Studio 中一键将 Keil 窗口置于前台
- **多版本支持**：支持 Keil uVision2/3/4/5 多个版本
- **智能检测**：自动检测正在运行的 Keil 进程
- **用户友好提示**：如果未检测到 Keil 运行，会提示用户启动 Keil
- **多种触发方式**：支持工具菜单点击和快捷键 Alt+K

## 安装方法

1. 下载 `.vsix` 扩展文件
2. 双击安装或通过 Visual Studio 的扩展管理器安装
3. 重启 Visual Studio

## 使用方法

1. **确保 Keil 已启动**：启动 Keil uVision 并打开您的项目
2. **触发扩展**：
   - 方式一：在 Visual Studio 中按 **Alt+K** 快捷键
   - 方式二：点击 **工具** → **编译Keil** 菜单项
3. **自动切换**：扩展会自动找到 Keil 实例并将其窗口置于前台

## 适用场景

- 嵌入式开发工作流程
- 需要频繁在 Visual Studio 和 Keil 之间切换的开发
- 使用 Visual Studio 进行代码编辑，使用 Keil 进行编译调试的场景

## 项目文件结构

```text
KeilBuilder/
├── KeilBuilder.sln                # Visual Studio扩展解决方案文件
├── KeilBuilder.csproj             # 扩展项目文件
├── KeilBuilderPackage.cs          # 主要扩展包类，实现IVsPackage接口
├── KeilBuilder.vsct               # 命令表文件，定义菜单和快捷键
├── source.extension.vsixmanifest  # VSIX扩展清单文件，定义扩展元数据和依赖
├── Anonymity.ico                  # 扩展图标文件
├── LICENSE.txt                    # 许可证文件
│
└── Properties/                    # 项目属性目录
    └── AssemblyInfo.cs            # 程序集信息文件，包含版本、版权等信息
```

## 技术实现

- **开发语言**：C#
- **目标框架**：.NET Framework 4.5+
- **Visual Studio 版本**：支持 VS 2022 (17.0+)
- **扩展类型**：VSIX Package

## 开发环境

- Visual Studio 2022
- .NET Framework 4.5 或更高版本
- Microsoft.VSSDK.BuildTools

## 许可证

本项目采用 MIT 许可证，详见 [LICENSE.txt](LICENSE.txt) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个扩展！