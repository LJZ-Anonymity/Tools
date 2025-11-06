# SoulKnightDataTool - 元气骑士数据工具

> 一个用于备份和还原元气骑士游戏数据的 Windows 批处理工具

灵感来源：https://github.com/HTCheater/SKDEditor

## 项目简介

SoulKnightDataTool 是一个专为元气骑士玩家设计的 Windows 批处理工具，用于方便地备份和还原游戏数据。通过 Android Debug Bridge (ADB)，该工具可以安全地备份和还原游戏存档，支持多版本游戏包名配置，并提供友好的命令行界面。

## 主要功能

- **数据备份** - 自动备份游戏数据文件到本地，支持时间戳备份
- **数据还原** - 将备份的数据还原到设备
- **配置管理** - 可自定义 ADB 路径、设备名、游戏包名、备份目录
- **彩色界面** - 友好的命令行界面，支持彩色输出
- **自动检测** - 自动检测可用设备列表
- **多版本支持** - 支持不同渠道版本的游戏包名配置

## 系统要求

- Windows 操作系统
- Android 设备（已 root 或有 root 权限）
- USB 调试已启用
- ADB 驱动已安装（首次使用需要安装 USB 驱动）

## 安装方法

1. 下载或克隆本仓库
2. 确保已安装所需的 USB 驱动（见下方说明）
3. 运行 `SoulKnightDataTool.bat` 启动工具

**首次连接设备时**，如果 Windows 无法自动识别设备，需要手动安装 USB 驱动：

1. 将设备通过 USB 连接到电脑
2. 按下 Win + X 打开设备管理器
3. 找到未识别的 Android 设备（可能显示为黄色感叹号）
4. 右键选择"更新驱动程序"
5. 选择"浏览计算机以查找驱动程序"
6. 选择 `usb_driver` 文件夹
7. 完成安装

## 使用方法

### 首次使用

1. 确保 Android 设备已开启 USB 调试模式
2. 将设备通过 USB 连接到电脑
3. 如果是首次连接，安装 USB 驱动（见安装方法）
4. 运行 `SoulKnightDataTool.bat`
5. 首次运行会自动生成配置文件 `config.bat`

### 配置设置

首次运行后，建议进行以下配置：

1. 在主菜单中选择 `3.设置`
2. 配置以下选项：
   - **ADB 路径**：默认指向 `ADB Files\adb.exe`，一般无需修改
   - **设备名**：选择要操作的设备（可从设备列表中选择）
   - **游戏包名**：默认 `com.chillyroom.knight.m4399`（4399版本）
   - **备份根目录**：设置备份文件的保存位置

> 💡 **提示**：修改设置时，直接回车可保持当前设置不变。

### 备份数据

1. 在主菜单中选择 `1.备份数据`
2. 工具会自动：
   - 检查设备连接状态
   - 创建带时间戳的备份目录（格式：`backup_YYYYMMDD_HHMM`）
   - 备份 `files` 目录下的所有文件
   - 备份 `shared_prefs` 目录下的配置文件
3. 备份完成后会显示成功/失败的文件数量

备份目录结构：

```text
备份根目录/
└── backup_YYYYMMDD_HHMM/
    ├── files/
    │   └── (游戏数据文件)
    └── shared_prefs/
        └── <包名>.v2.playerprefs.xml
```

### 还原数据

1. 在主菜单中选择 `2.还原数据`
2. 输入备份文件夹的名称（例如：`backup_20231201_1430`）
3. 工具会自动：
   - 检查备份文件夹是否存在
   - 还原 `files` 目录下的所有文件
   - 还原 `shared_prefs` 配置文件
4. 还原完成后会显示成功/失败/跳过的文件数量

> ⚠️ **注意**：还原操作需要设备具有 root 权限，否则会失败。

## 适用场景

- 游戏存档备份和还原
- 多设备存档同步
- 游戏数据迁移
- 存档版本管理

## 项目文件结构

```text
SoulKnightDataTool/
├── SoulKnightDataTool.bat       # 主程序文件，运行此文件启动工具
├── config.bat                   # 配置文件，首次运行自动生成
├── README.md                    # 项目说明文档
│
├── ADB Files/                   # ADB 工具文件目录
│   ├── adb.exe                  # ADB 可执行文件，用于与 Android 设备通信
│   ├── AdbWinApi.dll            # ADB Windows API 动态链接库
│   └── AdbWinUsbApi.dll         # ADB USB 通信 API 动态链接库
│
└── usb_driver/                  # USB 驱动程序目录
    ├── android_winusb.inf       # 驱动程序安装配置文件
    ├── androidwinusb86.cat      # 32位系统驱动程序签名文件
    ├── androidwinusba64.cat     # 64位系统驱动程序签名文件
    ├── i386/                    # 32位系统驱动文件目录
    ├── amd64/                   # 64位系统驱动文件目录
    └── source.properties        # 驱动源属性文件
```

> ⚠️ **注意**：`ADB Files` 文件夹中的文件是工具与 Android 设备通信的基础，请勿删除或移动。

## 配置文件说明

工具会在运行目录下自动生成 `config.bat` 配置文件，包含以下设置：

```batch
set "ADB=ADB Files\adb.exe"
set "DEVICE="
set "PACKAGE=com.ChillyRoom.DungeonShooter"
set "BACKUP_ROOT="
```

- `ADB` - ADB 可执行文件路径
- `DEVICE` - 设备名称（可通过 `adb devices` 查看）
- `PACKAGE` - 游戏包名
- `BACKUP_ROOT` - 备份文件根目录

## 技术实现

- **开发语言**：Windows 批处理脚本 (BAT)
- **运行环境**：Windows 操作系统
- **依赖工具**：Android Debug Bridge (ADB)
- **数据格式**：XML 配置文件 + 二进制游戏数据文件

## 常见问题

### Q: 提示"设备未连接"

A: 请检查：
- USB 调试是否已开启
- USB 连接是否正常
- 设备是否正确识别（可在设备管理器中查看）
- 设备名称是否与配置中的一致

### Q: 还原失败，提示需要 root 权限

A: 此工具需要设备已 root 或有 root 权限。部分操作需要访问 `/data/data/` 目录，需要 root 权限才能执行。

### Q: 如何切换游戏版本？

A: 在设置中修改"游戏包名"即可。不同版本的包名可能不同，例如：
- 官方版本：`com.ChillyRoom.DungeonShooter`
- vivo版本：`com.liangwu.yuanqiqishi.vivo`
- 4399版本：`com.chillyroom.knight.m4399`
- 九游版本：`com.knight.union.aligames`
- 华为版本：`yuanqiqishi.gamn.huawei`
- B站版本：`com.knight.union.bili`
- 其他渠道版本：请查看对应渠道的包名

### Q: 备份文件保存在哪里？

A: 备份文件保存在配置的"备份根目录"下，每个备份会自动创建带时间戳的文件夹。

## 注意事项

- ⚠️ **需要 root 权限**：还原操作需要设备具有 root 权限
- ⚠️ **数据安全**：备份和还原操作会修改游戏数据，请谨慎操作
- ⚠️ **备份建议**：在进行还原操作前，建议先备份当前数据
- ⚠️ **文件路径**：不要修改或移动 `ADB Files` 文件夹中的文件
- ⚠️ **驱动安装**：首次使用需要安装 USB 驱动，否则无法识别设备

## 许可证

本项目采用 MIT 许可证，详见根目录 [LICENSE](../LICENSE) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个工具！