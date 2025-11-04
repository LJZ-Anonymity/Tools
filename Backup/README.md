# Backup - 文件备份工具

> 一个用于备份文件和文件夹的 WPF 应用程序，备份过程使用 C++ DLL 实现高性能文件复制

## 项目简介

Backup 是一个基于 WPF 开发的 Windows 桌面应用程序，用于方便地备份和还原文件和文件夹。该应用程序采用 C# 开发用户界面，使用 C++ DLL 实现高性能的文件复制操作，支持自定义备份源和目标路径，并提供友好的图形化界面。

## 主要功能

- **文件/文件夹备份** - 支持选择单个文件或多个文件夹进行备份
- **自定义目标路径** - 可以自由选择备份文件的目标保存位置
- **高性能复制** - 使用 C++ DLL 实现，提供更快的文件复制速度
- **数据库管理** - 使用数据库记录备份信息，方便管理
- **图形化界面** - 友好的 WPF 界面，操作简单直观

## 安装方法

1. 下载或克隆本仓库
2. 使用 Visual Studio 打开 `Backup.sln` 解决方案
3. 编译并运行项目
4. 或直接运行编译后的可执行文件

## 使用方法

1. **启动应用程序**：运行编译后的 Backup.exe
2. **添加备份项**：在主窗口中点击添加按钮，选择要备份的文件或文件夹
3. **选择目标路径**：为每个备份项指定目标保存位置
4. **执行备份**：点击备份按钮开始备份操作
5. **管理备份**：可以查看、编辑或删除已添加的备份项

## 适用场景

- 重要文件定期备份
- 项目文件备份
- 配置文件备份
- 多位置文件同步

## 项目文件结构

```text
Backup/
├── Backup.sln                    # WPF主项目解决方案文件
├── Backup.csproj                 # WPF项目文件
├── Backup.csproj.user            # 用户项目配置文件
├── App.xaml                      # WPF应用程序主配置文件
├── App.xaml.cs                   # 应用程序代码后台文件
├── AssemblyInfo.cs               # 程序集信息文件
├── favicon.ico                   # 应用程序图标文件
├── favicon.png                   # 应用程序图标文件(PNG格式)
│
├── Database/                     # 数据库相关代码
│   └── FilesDatabase.cs          # 文件数据库操作类
│
├── Windows/                      # WPF窗口文件
│   ├── MainWindow.xaml           # 主窗口界面文件
│   ├── MainWindow.xaml.cs        # 主窗口代码后台文件
│   ├── AddWindow.xaml            # 添加窗口界面文件
│   └── AddWindow.xaml.cs         # 添加窗口代码后台文件
│
├── FileCopy/                     # C++ DLL项目
│   ├── FileCopy.sln              # C++项目解决方案文件
│   ├── FileCopy.vcxproj          # C++项目文件
│   ├── FileCopy.vcxproj.filters  # 项目文件过滤器
│   ├── FileCopy.vcxproj.user     # 用户项目配置文件
│   ├── FileCopy.cpp              # 主要C++实现文件
│   ├── pch.h                     # 预编译头文件
│   ├── pch.cpp                   # 预编译头文件实现
│   └── framework.h               # 框架头文件
│
├── Resources/                    # 资源文件目录
│   └── Styles/                   # 样式文件目录
│       ├── ButtonStyles.xaml     # 按钮样式定义
│       ├── TextBoxStyle.xaml     # 文本框样式定义
│       ├── ScrollBarStyle.xaml   # 滚动条样式定义
│       ├── TooltipStyle.xaml     # 工具提示样式定义
│       └── CheckBoxStyle.xaml    # 复选框样式定义
│
└── Properties/                   # 项目属性目录
```

## 技术实现

- **开发语言**：C# (WPF) + C++ (DLL)
- **目标框架**：.NET Framework（WPF 项目）
- **UI 框架**：Windows Presentation Foundation (WPF)
- **文件复制**：C++ DLL 实现高性能文件操作
- **数据存储**：数据库存储备份配置信息

## 开发环境

- Visual Studio 2022
- .NET Framework（适用于 WPF 项目）
- C++ 编译器（用于编译 FileCopy DLL）

## 许可证

本项目采用 MIT 许可证，详见根目录 [LICENSE](../LICENSE) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个工具！
