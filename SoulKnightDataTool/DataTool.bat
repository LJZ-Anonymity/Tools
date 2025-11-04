@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

:: ESC字符定义
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
    call :init_config_file
)

:: 读取配置文件
call config.bat

:: 检查ADB
if not exist "%ADB%" (
    echo %RED%ADB未找到：%ADB%%RESET%
    echo.
    pause & goto menu
)

:menu
echo %YELLOW%┌───────────────────────────┐%RESET%
echo %YELLOW%│   1.备份数据              │%RESET%
echo %YELLOW%│   2.还原数据              │%RESET%
echo %YELLOW%│   3.设置                  │%RESET%
echo %YELLOW%│   4.退出                  │%RESET%
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
echo %YELLOW%│   1.修改ADB路径           │%RESET%
echo %YELLOW%│   2.修改设备名            │%RESET%
echo %YELLOW%│   3.修改游戏包名          │%RESET%
echo %YELLOW%│   4.修改目标文件路径      │%RESET%
echo %YELLOW%│   5.修改备份根目录        │%RESET%
echo %YELLOW%│   6.返回主菜单            │%RESET%
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

:: 修改ADB路径
:modify_adb_path
set /p new_adb_path=请输入新的ADB路径：
call :update_config ADB "%new_adb_path%"
echo ADB路径已修改为：%new_adb_path%
echo.
goto settingmenu

:: 修改设备名
:modify_device_name
set /p new_device_name=请输入新的设备名称：
call :update_config DEVICE "%new_device_name%"
echo 设备名称已修改为：%new_device_name%
echo.
goto settingmenu

:: 修改游戏包名
:modify_package_name
set /p new_package_name=请输入新的游戏包名：
call :update_config PACKAGE "%new_package_name%"
echo 游戏包名已修改为：%new_package_name%
echo.
goto settingmenu

:: 修改目标文件路径
:modify_target_path
set /p new_target_path=请输入新的目标文件路径：
call :update_config TARGET "%new_target_path%"
echo 目标文件路径已修改为：%new_target_path%
echo.
goto settingmenu

:: 修改备份根目录
:modify_backup_root
set /p new_backup_root=请输入新的备份根目录：
call :update_config BACKUP_ROOT "%new_backup_root%"
echo 备份根目录已修改为：%new_backup_root%
echo.
goto settingmenu

:: 初始化配置文件函数
:init_config_file
echo @echo off > config.bat
echo set "ADB=ADB Files\adb.exe" >> config.bat
echo set "DEVICE=emulator-5554" >> config.bat
echo set "PACKAGE=com.chillyroom.knight.m4399" >> config.bat
echo set "TARGET=shared_prefs/%%PACKAGE%%.v2.playerprefs.xml" >> config.bat
echo set "BACKUP_ROOT=E:\Game\SoulKnight\Saves" >> config.bat
attrib +h config.bat 2>nul
exit /b

:: 更新配置文件的辅助函数
:update_config
set "CONFIG_KEY=%~1"
set "CONFIG_VALUE=%~2"
if not exist "config.bat" (
    call :init_config_file
)
:: 读取现有配置
setlocal EnableDelayedExpansion
set "NEW_ADB=!ADB!"
set "NEW_DEVICE=!DEVICE!"
set "NEW_PACKAGE=!PACKAGE!"
set "NEW_TARGET=!TARGET!"
set "NEW_BACKUP_ROOT=!BACKUP_ROOT!"
:: 更新对应的配置项
if /i "!CONFIG_KEY!"=="ADB" set "NEW_ADB=!CONFIG_VALUE!"
if /i "!CONFIG_KEY!"=="DEVICE" set "NEW_DEVICE=!CONFIG_VALUE!"
if /i "!CONFIG_KEY!"=="PACKAGE" set "NEW_PACKAGE=!CONFIG_VALUE!"
if /i "!CONFIG_KEY!"=="TARGET" set "NEW_TARGET=!CONFIG_VALUE!"
if /i "!CONFIG_KEY!"=="BACKUP_ROOT" set "NEW_BACKUP_ROOT=!CONFIG_VALUE!"
:: 写入新配置
echo @echo off > config.bat
echo set "ADB=!NEW_ADB!" >> config.bat
echo set "DEVICE=!NEW_DEVICE!" >> config.bat
echo set "PACKAGE=!NEW_PACKAGE!" >> config.bat
echo set "TARGET=!NEW_TARGET!" >> config.bat
echo set "BACKUP_ROOT=!NEW_BACKUP_ROOT!" >> config.bat
attrib +h config.bat 2>nul
endlocal
exit /b

:backup
echo.

:: 检查设备
call :check_device

:: 生成时间戳目录
for /f "tokens=2 delims==" %%a in ('wmic os get localdatetime /value') do set DT=%%a
set "TS=%DT:~0,8%_%DT:~8,4%"
set "BACKUP_DIR=%BACKUP_ROOT%\backup_%TS%"
md "%BACKUP_DIR%" 2>nul

:: 循环处理每个文件
set "FILE_INDEX=0"
set "SUCCESS_COUNT=0"
set "FAIL_COUNT=0"
call :process_backup_file "files/game.data" "files" "game.data"
call :process_backup_file "files/item_data.data" "files" "item_data.data"
call :process_backup_file "files/season_data.data" "files" "season_data.data"
call :process_backup_file "files/setting.data" "files" "setting.data"
call :process_backup_file "files/statistics.data" "files" "statistics.data"
call :process_backup_file "files/task.data" "files" "task.data"
call :process_backup_file "files/battles.data" "files" "battles.data"
call :process_backup_file "files/net_battle.data" "files" "net_battle.data"
call :process_backup_file "files/sandbox_config.data" "files" "sandbox_config.data"
call :process_backup_file "files/sandbox_maps.data" "files" "sandbox_maps.data"
call :process_backup_file "files/backup.data" "files" "backup.data"
call :process_backup_file "files/mall_reload_data.data" "files" "mall_reload_data.data"
call :process_backup_file "files/monsrise_data.data" "files" "monsrise_data.data"
call :process_backup_file "shared_prefs/%PACKAGE%.v2.playerprefs.xml" "shared_prefs" "%PACKAGE%.v2.playerprefs.xml"
goto :backup_done

:process_backup_file
set "SOURCE_PATH=%~1"
set "TARGET_FOLDER=%~2"
set "TARGET_NAME=%~3"

:: 创建目标文件夹
for %%D in ("%BACKUP_DIR%") do set "TARGET_DIR=%%~D\!TARGET_FOLDER!"
if not exist "!TARGET_DIR!" (
    md "!TARGET_DIR!" 2>nul
)

:: 生成唯一的中转文件名
set /a FILE_INDEX+=1
set "TMP_SDCARD=/sdcard/sk_tmp_!FILE_INDEX!"
echo 正在备份 !SOURCE_PATH! ...
"%ADB%" -s %DEVICE% shell "su -c 'cp /data/data/%PACKAGE%/!SOURCE_PATH! !TMP_SDCARD!'" >nul 2>&1
if not errorlevel 1 (
    :: 拉取文件
    set "TARGET_FILE=!TARGET_DIR!\!TARGET_NAME!"
    "%ADB%" -s %DEVICE% pull !TMP_SDCARD! "!TARGET_FILE!" >nul 2>&1
    if not errorlevel 1 (
        set /a SUCCESS_COUNT+=1
    ) else (
        echo %RED%拉取失败：!SOURCE_PATH!%RESET%
        set /a FAIL_COUNT+=1
    )
    :: 清理中转
    "%ADB%" -s %DEVICE% shell "rm !TMP_SDCARD!" >nul 2>&1
) else (
    echo %YELLOW%文件不存在或无法访问：!SOURCE_PATH!%RESET%
    set /a FAIL_COUNT+=1
)
exit /b

:backup_done

:: 提取目录信息
for %%A in ("%BACKUP_DIR%") do (
    set "PARENT_DIR=%%~dpA"
    set "FOLDER_NAME=%%~nxA"
)

:: 修改输出显示
echo %GREEN%备份完成！%RESET%
echo 成功：%SUCCESS_COUNT% 个文件，失败：%FAIL_COUNT% 个文件
echo 文件已保存至：%PARENT_DIR%%GREEN%%FOLDER_NAME%%RESET%
echo.
goto menu

:restore
echo.

:: 检查设备
call :check_device

:: 让用户输入备份文件夹名称
set /p "FOLDER_NAME=请输入要还原文件所在文件夹的名称："
set "SELECTED=!BACKUP_ROOT!\!FOLDER_NAME!"
if not exist "!SELECTED!" (
    echo %RED%备份文件夹不存在：!SELECTED!%RESET%
    echo.
    pause & goto menu
)

:: 循环处理每个文件
set "FILE_INDEX=0"
set "SUCCESS_COUNT=0"
set "FAIL_COUNT=0"
set "SKIP_COUNT=0"
call :process_restore_file "files/game.data" "files" "game.data"
call :process_restore_file "files/item_data.data" "files" "item_data.data"
call :process_restore_file "files/season_data.data" "files" "season_data.data"
call :process_restore_file "files/setting.data" "files" "setting.data"
call :process_restore_file "files/statistics.data" "files" "statistics.data"
call :process_restore_file "files/task.data" "files" "task.data"
call :process_restore_file "files/battles.data" "files" "battles.data"
call :process_restore_file "files/net_battle.data" "files" "net_battle.data"
call :process_restore_file "files/sandbox_config.data" "files" "sandbox_config.data"
call :process_restore_file "files/sandbox_maps.data" "files" "sandbox_maps.data"
call :process_restore_file "files/backup.data" "files" "backup.data"
call :process_restore_file "files/mall_reload_data.data" "files" "mall_reload_data.data"
call :process_restore_file "files/monsrise_data.data" "files" "monsrise_data.data"
call :process_restore_file "shared_prefs/%PACKAGE%.v2.playerprefs.xml" "shared_prefs" "%PACKAGE%.v2.playerprefs.xml"
goto :restore_done

:process_restore_file
set "SOURCE_PATH=%~1"
set "TARGET_FOLDER=%~2"
set "TARGET_NAME=%~3"

:: 生成唯一的中转文件名（始终递增，避免冲突）
set /a FILE_INDEX+=1
set "TMP_SDCARD=/sdcard/sk_restore_!FILE_INDEX!"

:: 确认文件存在
set "RESTORE_FILE=!SELECTED!\!TARGET_FOLDER!\!TARGET_NAME!"
if exist "!RESTORE_FILE!" (
    echo 正在还原 !SOURCE_PATH! ...
    "%ADB%" -s %DEVICE% push "!RESTORE_FILE!" !TMP_SDCARD! >nul 2>&1
    if not errorlevel 1 (
        :: 复制到目标路径（需root）
        "%ADB%" -s %DEVICE% shell "su -c 'cp !TMP_SDCARD! /data/data/%PACKAGE%/!SOURCE_PATH! && chmod 660 /data/data/%PACKAGE%/!SOURCE_PATH!'" >nul 2>&1
        if not errorlevel 1 (
            set /a SUCCESS_COUNT+=1
        ) else (
            echo %RED%还原失败：!SOURCE_PATH!，请确认root权限%RESET%
            set /a FAIL_COUNT+=1
        )
        :: 清理中转
        "%ADB%" -s %DEVICE% shell "rm !TMP_SDCARD!" >nul 2>&1
    ) else (
        echo %RED%推送失败：!SOURCE_PATH!%RESET%
        set /a FAIL_COUNT+=1
    )
) else (
    set /a SKIP_COUNT+=1
)
exit /b

:restore_done

echo %GREEN%还原完成！%RESET%
echo 成功：%SUCCESS_COUNT% 个文件，失败：%FAIL_COUNT% 个文件，跳过：%SKIP_COUNT% 个文件
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
