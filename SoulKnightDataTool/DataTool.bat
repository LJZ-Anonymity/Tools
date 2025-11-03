@echo off
setlocal EnableExtensions
chcp 65001 >nul

:: 新增ESC字符定义 (添加在标题设置后)
for /F "tokens=2 delims=#" %%a in ('"prompt #$H#$E# & echo on & for %%b in (1) do rem"') do set "ESC=%%a"

:: 定义颜色代码
set "GREEN=%ESC%[32m"  :: 绿色
set "RED=%ESC%[31m"    :: 红色
set "YELLOW=%ESC%[33m" :: 黄色
set "RESET=%ESC%[0m"   :: 重置(默认颜色 灰色)

title 元气骑士数据工具
cd /d "%~dp0"

:: 生成并读取配置文件
if not exist "config.bat" (
    echo @echo off > config.bat
    echo set "ADB=ADB Files\adb.exe" >> config.bat
    echo set "DEVICE=emulator-5554" >> config.bat
    echo set "PACKAGE=com.chillyroom.knight.m4399" >> config.bat
    echo set "TARGET=shared_prefs/%%PACKAGE%%.v2.playerprefs.xml" >> config.bat
    echo set "BACKUP_ROOT=E:\Game\SoulKnight\Saves" >> config.bat
    attrib +h config.bat :: 设置config.bat为隐藏文件
)

call config.bat :: 读取配置文件

:: 检查ADB
if not exist "%ADB%" (
    echo %RED%ADB未找到：%ADB%%RESET%
    echo.
    pause & goto menu
)

:menu
echo %YELLOW%┌───────────────────────────┐%RESET%
echo %YELLOW%│                           │%RESET%
echo %YELLOW%│   1.备份数据              │%RESET%
echo %YELLOW%│   2.还原数据              │%RESET%
echo %YELLOW%│   3.设置                  │%RESET%
echo %YELLOW%│   4.退出                  │%RESET%
echo %YELLOW%│                           │%RESET%
echo %YELLOW%└───────────────────────────┘%RESET%
set /p choice=请选择操作(1/2/3/4): 
echo.

if "%choice%"=="1" goto backup
if "%choice%"=="2" goto restore
if "%choice%"=="3" goto settingmenu
if "%choice%"=="4" goto end
echo.
echo %RED%无效选择，请重新输入。%RESET%
echo.
goto menu

:settingmenu
echo %YELLOW%┌───────────────────────────┐%RESET%
echo %YELLOW%│                           │%RESET%
echo %YELLOW%│   1.修改ADB路径           │%RESET%
echo %YELLOW%│   2.修改设备名            │%RESET%
echo %YELLOW%│   3.修改游戏包名          │%RESET%
echo %YELLOW%│   4.修改目标文件路径      │%RESET%
echo %YELLOW%│   5.修改备份根目录        │%RESET%
echo %YELLOW%│   6.返回主菜单            │%RESET%
echo %YELLOW%│                           │%RESET%
echo %YELLOW%└───────────────────────────┘%RESET%
set /p setting_choice=请选择操作(1/2/3/4/5/6): 
echo.

if "%setting_choice%"=="1" goto modify_adb_path
if "%setting_choice%"=="2" goto modify_device_name
if "%setting_choice%"=="3" goto modify_package_name
if "%setting_choice%"=="4" goto modify_target_path
if "%setting_choice%"=="5" goto modify_backup_root
if "%setting_choice%"=="6" goto menu
echo.
echo %RED%无效选择，请重新输入。%RESET%
echo.
goto settingmenu

:modify_adb_path
set /p new_adb_path=请输入新的ADB路径：
echo set "ADB=%new_adb_path%" > config.bat
echo ADB路径已修改为：%new_adb_path%
pause
goto settingmenu

:modify_device_name
set /p new_device_name=请输入新的设备名称：
echo set "DEVICE=%new_device_name%" > config.bat
echo 设备名称已修改为：%new_device_name%
pause
goto settingmenu

:modify_package_name
set /p new_package_name=请输入新的游戏包名：
echo set "PACKAGE=%new_package_name%" > config.bat
echo 游戏包名已修改为：%new_package_name%
pause
goto settingmenu

:modify_target_path
set /p new_target_path=请输入新的目标文件路径：
echo set "TARGET=%new_target_path%" > config.bat
echo 目标文件路径已修改为：%new_target_path%
pause
goto settingmenu

:modify_backup_root
set /p new_backup_root=请输入新的备份根目录：
echo set "BACKUP_ROOT=%new_backup_root%" > config.bat
echo 备份根目录已修改为：%new_backup_root%
pause
goto settingmenu

:backup
echo.

:: 检查设备
call :check_device

:: 生成时间戳目录
for /f "tokens=2 delims==" %%a in ('wmic os get localdatetime /value') do set DT=%%a
set "TS=%DT:~0,8%_%DT:~8,4%"
set "BACKUP_DIR=%BACKUP_ROOT%\backup_%TS%"
md "%BACKUP_DIR%" 2>nul

:: 复制到/sdcard中转（需root）
set "TMP_SDCARD=/sdcard/sk_tmp.xml"
echo 正在复制到中转区...
"%ADB%" -s %DEVICE% shell "su -c 'cp /data/data/%PACKAGE%/%TARGET% %TMP_SDCARD%'"
if errorlevel 1 (
    echo %RED%获取文件失败，请确认root权限%RESET%
    echo.
    pause & goto menu
)

:: 拉取文件
echo 正在拉取文件...
"%ADB%" -s %DEVICE% pull %TMP_SDCARD% "%BACKUP_DIR%\%PACKAGE%.v2.playerprefs.xml"
if errorlevel 1 (
    echo %RED%拉取失败%RESET%
    echo.
    pause & goto menu
)

:: 清理中转
"%ADB%" -s %DEVICE% shell "rm %TMP_SDCARD%"

:: 提取目录信息
for %%A in ("%BACKUP_DIR%") do (
    set "PARENT_DIR=%%~dpA"
    set "FOLDER_NAME=%%~nxA"
)

:: 修改输出显示
echo %GREEN%备份完成！%RESET%文件已保存至：
<nul set /p=   %PARENT_DIR%
<nul set /p=%GREEN%%FOLDER_NAME%%RESET%
echo \%PACKAGE%.v2.playerprefs.xml
echo.
echo.
goto menu

:restore
echo.

:: 检查设备
call :check_device

:: 让用户输入备份文件夹名称
set /p "FOLDER_NAME=请输入要还原文件所在文件夹的名称："
set "SELECTED=%BACKUP_ROOT%\%FOLDER_NAME%"
if not exist "%SELECTED%" (
    echo %RED%备份文件夹不存在：%SELECTED%%RESET%
    echo.
    pause & goto menu
)

:: 确认文件存在
set "RESTORE_FILE=%SELECTED%\%PACKAGE%.v2.playerprefs.xml"
if not exist "%RESTORE_FILE%" (
    echo %RED%备份文件不存在：%RESTORE_FILE%%RESET%
    echo.
    pause & goto menu
)

:: 推送到/sdcard中转
set "TMP_SDCARD=/sdcard/sk_restore.xml"
echo 正在推送文件到设备...
"%ADB%" -s %DEVICE% push "%RESTORE_FILE%" %TMP_SDCARD%
if errorlevel 1 (
    echo %RED%推送失败%RESET%
    echo.
    pause & goto menu
)

:: 复制到目标路径（需root）
echo 正在写入游戏目录...
"%ADB%" -s %DEVICE% shell "su -c 'cp %TMP_SDCARD% /data/data/%PACKAGE%/%TARGET% && chmod 660 /data/data/%PACKAGE%/%TARGET%'"
if errorlevel 1 (
    echo %RED%还原失败，请确认root权限%RESET%
    echo.
    pause & goto menu
)

:: 清理中转
"%ADB%" -s %DEVICE% shell "rm %TMP_SDCARD%"

echo %GREEN%还原完成！%RESET%
echo.
goto menu

:end
echo 已退出。
exit /b

:: 检查设备
:check_device
"%ADB%" devices | findstr /b /c:"%DEVICE%" >nul
if errorlevel 1 (
    echo %RED%设备 %DEVICE% 未连接%RESET%
    echo.
    pause & goto menu
)
exit /b
