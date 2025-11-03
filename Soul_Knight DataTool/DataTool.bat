@echo off
setlocal EnableExtensions
chcp 65001 >nul

:: 新增ESC字符定义 (添加在标题设置后)
for /F "tokens=2 delims=#" %%a in ('"prompt #$H#$E# & echo on & for %%b in (1) do rem"') do set "ESC=%%a"

:: 定义颜色代码
set "GREEN=%ESC%[32m"
set "RED=%ESC%[31m"
set "YELLOW=%ESC%[33m"
set "RESET=%ESC%[0m"

title 元气骑士数据工具
cd /d "%~dp0"

:: 配置
set "ADB=E:\Program\ADB\adb.exe"
set "DEVICE=emulator-5554"
set "PACKAGE=com.chillyroom.knight.m4399"
set "TARGET=shared_prefs/%PACKAGE%.v2.playerprefs.xml"
set "BACKUP_ROOT=E:\Game\SoulKnight\Saves"

:: 检查ADB
if not exist "%ADB%" (
    echo %RED%ADB未找到：%ADB%%RESET%
    pause & goto menu
)

:menu
echo %YELLOW%┌───────────────────────────┐%RESET%
echo %YELLOW%│                           │%RESET%
echo %YELLOW%│   1.备份数据              │%RESET%
echo %YELLOW%│   2.还原数据              │%RESET%
echo %YELLOW%│   3.退出                  │%RESET%
echo %YELLOW%│                           │%RESET%
echo %YELLOW%└───────────────────────────┘%RESET%
set /p choice=请选择操作(1/2/3): 

if "%choice%"=="1" goto backup
if "%choice%"=="2" goto restore
if "%choice%"=="3" goto end
echo.
echo %RED%无效选择，请重新输入。%RESET%
echo.
goto menu

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
    pause & goto menu
)

:: 拉取文件
echo 正在拉取文件...
"%ADB%" -s %DEVICE% pull %TMP_SDCARD% "%BACKUP_DIR%\%PACKAGE%.v2.playerprefs.xml"
if errorlevel 1 (
    echo %RED%拉取失败%RESET%
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
set /p "FOLDER_NAME=请输入要还原的备份文件夹名称："
set "SELECTED=%BACKUP_ROOT%\%FOLDER_NAME%"
if not exist "%SELECTED%" (
    echo %RED%备份文件夹不存在：%SELECTED%%RESET%
    pause & goto menu
)

:: 确认文件存在
set "RESTORE_FILE=%SELECTED%\%PACKAGE%.v2.playerprefs.xml"
if not exist "%RESTORE_FILE%" (
    echo %RED%备份文件不存在：%RESTORE_FILE%%RESET%
    pause & goto menu
)

:: 推送到/sdcard中转
set "TMP_SDCARD=/sdcard/sk_restore.xml"
echo 正在推送文件到设备...
"%ADB%" -s %DEVICE% push "%RESTORE_FILE%" %TMP_SDCARD%
if errorlevel 1 (
    echo %RED%推送失败%RESET%
    pause & goto menu
)

:: 复制到目标路径（需root）
echo 正在写入游戏目录...
"%ADB%" -s %DEVICE% shell "su -c 'cp %TMP_SDCARD% /data/data/%PACKAGE%/%TARGET% && chmod 660 /data/data/%PACKAGE%/%TARGET%'"
if errorlevel 1 (
    echo %RED%还原失败，请确认root权限%RESET%
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

:check_device
"%ADB%" devices | findstr /b /c:"%DEVICE%" >nul
if errorlevel 1 (
    echo %RED%设备 %DEVICE% 未连接%RESET%
    pause & goto menu
)
exit /b
