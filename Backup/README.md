备份文件的WPF应用程序,备份过程使用的是C++的 dll

可以自己选择备份的文件/文件夹以及目标地址

项目文件结构：

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