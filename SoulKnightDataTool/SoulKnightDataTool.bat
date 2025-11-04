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
:: 处理files目录下的所有子文件
call :process_backup_files_folder
call :process_backup_file "shared_prefs/%PACKAGE%.v2.playerprefs.xml" "shared_prefs" "%PACKAGE%.v2.playerprefs.xml"
goto :backup_done

:: 处理files目录下的所有子文件
:process_backup_files_folder
:: 创建目标文件夹
for %%D in ("%BACKUP_DIR%") do set "TARGET_DIR=%%~D\files"
if not exist "!TARGET_DIR!" (
    md "!TARGET_DIR!" 2>nul
)

:: 获取文件列表
set "FILES_LIST=%TEMP%\sk_files_list.txt"
:: 清理设备上可能残留的临时文件
"%ADB%" -s %DEVICE% shell "rm -f /sdcard/files_list.txt" >nul 2>&1

:: 获取文件列表（使用ls -p列出文件，以/结尾的是目录，过滤掉）
"%ADB%" -s %DEVICE% shell "su -c 'ls -p /data/data/%PACKAGE%/files/ | grep -v /' > /sdcard/files_list.txt" >nul 2>&1
if errorlevel 1 (
    :: 尝试不使用su
    "%ADB%" -s %DEVICE% shell "ls -p /data/data/%PACKAGE%/files/ | grep -v / > /sdcard/files_list.txt" >nul 2>&1
    if errorlevel 1 (
        :: 备用方法：使用find命令
        "%ADB%" -s %DEVICE% shell "su -c 'find /data/data/%PACKAGE%/files -maxdepth 1 -type f -exec basename {} \;' > /sdcard/files_list.txt" >nul 2>&1
        if errorlevel 1 (
            "%ADB%" -s %DEVICE% shell "find /data/data/%PACKAGE%/files -maxdepth 1 -type f -exec basename {} \; > /sdcard/files_list.txt" >nul 2>&1
        )
    )
)

:: 拉取文件列表到本地
"%ADB%" -s %DEVICE% pull /sdcard/files_list.txt "!FILES_LIST!" >nul 2>&1

if exist "!FILES_LIST!" (
    :: 读取文件列表并逐个处理
    for /f "usebackq delims=" %%L in ("!FILES_LIST!") do (
        set "FILE_NAME=%%L"
        :: 去除首尾空格（使用for循环去除空格）
        for /f "tokens=*" %%N in ("!FILE_NAME!") do set "FILE_NAME=%%N"
        if not "!FILE_NAME!"=="" (
            :: 生成唯一的中转文件名
            set /a FILE_INDEX+=1
            set "TMP_SDCARD=/sdcard/sk_tmp_!FILE_INDEX!"
            
            echo 正在备份 files/!FILE_NAME! ...
            :: 复制文件到sdcard
            "%ADB%" -s %DEVICE% shell "su -c 'cp /data/data/%PACKAGE%/files/!FILE_NAME! !TMP_SDCARD!'" >nul 2>&1
            if not errorlevel 1 (
                :: 拉取文件
                set "TARGET_FILE=!TARGET_DIR!\!FILE_NAME!"
                "%ADB%" -s %DEVICE% pull !TMP_SDCARD! "!TARGET_FILE!" >nul 2>&1
                if not errorlevel 1 (
                    set /a SUCCESS_COUNT+=1
                ) else (
                    echo %RED%拉取失败：files/!FILE_NAME!%RESET%
                    set /a FAIL_COUNT+=1
                )
                :: 清理中转
                "%ADB%" -s %DEVICE% shell "rm !TMP_SDCARD!" >nul 2>&1
            ) else (
                echo %YELLOW%文件不存在或无法访问：files/!FILE_NAME!%RESET%
                set /a FAIL_COUNT+=1
            )
        )
    )
    del "!FILES_LIST!" >nul 2>&1
    "%ADB%" -s %DEVICE% shell "rm /sdcard/files_list.txt" >nul 2>&1
) else (
    echo %YELLOW%警告：无法获取files目录下的文件列表%RESET%
)
exit /b

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

:: 提取目录信息（分离父目录和文件夹名称）
for %%A in ("!BACKUP_DIR!") do (
    set "PARENT_PATH=%%~dpA"
    set "FOLDER_NAME=%%~nxA"
)

:: 修改输出显示
echo %GREEN%备份完成！%RESET%
echo 成功：%SUCCESS_COUNT% 个文件，失败：%FAIL_COUNT% 个文件%RESET%
echo %RESET%文件已保存至：!PARENT_PATH!%GREEN%!FOLDER_NAME!%RESET%
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
:: 处理files目录下的所有文件（仅文件，不含子文件夹）
call :process_restore_files_folder
call :process_restore_file "shared_prefs/%PACKAGE%.v2.playerprefs.xml" "shared_prefs" "%PACKAGE%.v2.playerprefs.xml"
goto :restore_done

:: 处理files目录下的所有文件（仅文件，不含子文件夹）
:process_restore_files_folder
set "FILES_SOURCE_DIR=!SELECTED!\files"
if not exist "!FILES_SOURCE_DIR!" (
    echo %YELLOW%备份目录中不存在files文件夹，跳过files目录还原%RESET%
    exit /b
)

:: 扫描files文件夹下的所有文件（只扫描第一层，不包括子文件夹）
for %%F in ("!FILES_SOURCE_DIR!\*") do (
    :: 检查是否为文件（目录判断：如果路径末尾加\后仍存在，则为目录）
    if not exist "%%F\" (
        set "FILE_NAME=%%~nxF"
        if not "!FILE_NAME!"=="" (
            :: 生成唯一的中转文件名（始终递增，避免冲突）
            set /a FILE_INDEX+=1
            set "TMP_SDCARD=/sdcard/sk_restore_!FILE_INDEX!"
            
            echo 正在还原 files/!FILE_NAME! ...
            :: 推送文件到设备
            "%ADB%" -s %DEVICE% push "%%F" !TMP_SDCARD! >nul 2>&1
            if not errorlevel 1 (
                :: 复制到目标路径（需root）
                "%ADB%" -s %DEVICE% shell "su -c 'cp !TMP_SDCARD! /data/data/%PACKAGE%/files/!FILE_NAME! && chmod 660 /data/data/%PACKAGE%/files/!FILE_NAME!'" >nul 2>&1
                if not errorlevel 1 (
                    set /a SUCCESS_COUNT+=1
                ) else (
                    echo %RED%还原失败：files/!FILE_NAME!，请确认root权限%RESET%
                    set /a FAIL_COUNT+=1
                )
                :: 清理中转
                "%ADB%" -s %DEVICE% shell "rm !TMP_SDCARD!" >nul 2>&1
            ) else (
                echo %RED%推送失败：files/!FILE_NAME!%RESET%
                set /a FAIL_COUNT+=1
            )
        )
    )
)
exit /b

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
"%ADB%" devices | findstr /b /c:"!DEVICE!" >nul
if errorlevel 1 (
    echo %RED%设备 !DEVICE! 未连接%RESET%
    echo.
    pause & goto menu
)
exit /b
