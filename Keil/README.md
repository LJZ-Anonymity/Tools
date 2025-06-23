Keil 代码编译的扩展

项目文件结构：
```text
Keil/
├── Keil.sln                      # Visual Studio扩展解决方案文件
├── Keil.csproj                   # 扩展项目文件
├── KeilPackage.cs                # 主要扩展包类，实现IVsPackage接口
├── source.extension.vsixmanifest # VSIX扩展清单文件，定义扩展元数据和依赖
│
└── Properties/                    # 项目属性目录
    └── AssemblyInfo.cs           # 程序集信息文件，包含版本、版权等信息
```