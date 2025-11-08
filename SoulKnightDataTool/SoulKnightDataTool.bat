@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

:: ESC字符定义
for /F "tokens=2 delims=#" %%a in ('"prompt #$H#$E# & echo on & for %%b in (1) do rem"') do set "ESC=%%a"

:: 定义颜色代码
set "GREEN=%ESC%[32m"
set "RED=%ESC%[31m"
set "YELLOW=%ESC%[33m"
set "RESET=%ESC%[0m"

title 元气骑士数据工具
cd /d "%~dp0"

:: 生成并读取配置文件
if not exist "config.bat" (
    call :initcfg
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
echo 当前备份根目录：%GREEN%!BACKUP_ROOT!%RESET%
echo 当前游戏包名：%GREEN%!PACKAGE!%RESET%
echo 当前存档ID：%GREEN%!SAVE_ID!%RESET%
echo 当前设备：%GREEN%%DEVICE%%RESET%
set /p choice=请选择操作(1/2/3/4): 
echo.

if "%choice%"=="1" goto backup
if "%choice%"=="2" goto restore
if "%choice%"=="3" goto settingmenu
if "%choice%"=="4" goto end
echo %RED%无效选择，请重新输入。%RESET%
pause
echo.
goto menu

:settingmenu
echo %YELLOW%┌───────────────────────────┐%RESET%
echo %YELLOW%│   1.修改ADB路径           │%RESET%
echo %YELLOW%│   2.修改设备名            │%RESET%
echo %YELLOW%│   3.修改游戏包名          │%RESET%
echo %YELLOW%│   4.修改备份根目录        │%RESET%
echo %YELLOW%│   5.修改存档ID            │%RESET%
echo %YELLOW%│   6.返回主菜单            │%RESET%
echo %YELLOW%└───────────────────────────┘%RESET%
set /p setting_choice=请选择操作(1/2/3/4/5/6): 
echo.

if "%setting_choice%"=="1" goto modifyadb
if "%setting_choice%"=="2" goto modifydev
if "%setting_choice%"=="3" goto modifypkg
if "%setting_choice%"=="4" goto modifyroot
if "%setting_choice%"=="5" goto modifyuid
if "%setting_choice%"=="6" goto menu
echo %RED%无效选择，请重新输入。%RESET%
pause
echo.
goto settingmenu

:: 修改ADB路径

:modifyadb
echo 当前ADB路径：%GREEN%!ADB!%RESET%
set "new_adb_path="
set /p new_adb_path=请输入新的ADB路径（直接回车保持当前设置）：
if not defined new_adb_path (
    echo ADB路径未修改，仍为：%GREEN%!ADB!%RESET%
) else (
    :: 去除前后空格
    for /f "tokens=*" %%a in ("!new_adb_path!") do set "new_adb_path=%%a"
    call :updatecfg ADB "!new_adb_path!"
    echo ADB路径已修改为：%GREEN%!new_adb_path!%RESET%
)
echo.
goto settingmenu

:: 修改设备名

:modifydev
echo 当前设备：%GREEN%!DEVICE!%RESET%
echo 正在获取可用设备列表...
echo.
"%ADB%" devices | findstr /v "^List of devices" | findstr /v "^$"
echo.
set "new_device_name="
set /p new_device_name=请输入新的设备名称（直接回车保持当前设备）：
if not defined new_device_name (
    echo 设备名称未修改，仍为：%GREEN%!DEVICE!%RESET%
) else (
    :: 去除前后空格
    for /f "tokens=*" %%a in ("!new_device_name!") do set "new_device_name=%%a"
    call :updatecfg DEVICE "!new_device_name!"
    echo 设备名称已修改为：%GREEN%!new_device_name!%RESET%
)
echo.
goto settingmenu

:: 修改游戏包名

:modifypkg
echo 当前游戏包名：%GREEN%!PACKAGE!%RESET%
echo 官方版本    com.ChillyRoom.DungeonShooter
echo vivo版本    com.liangwu.yuanqiqishi.vivo
echo 4399版本    com.chillyroom.knight.m4399
echo 九游版本    com.knight.union.aligames
echo 华为版本    yuanqiqishi.gamn.huawei
echo B站版本     com.knight.union.bili
echo.
set "new_package_name="
set /p new_package_name=请输入新的游戏包名（直接回车保持当前设置）：
if not defined new_package_name (
    echo 游戏包名未修改，仍为：%GREEN%!PACKAGE!%RESET%
) else (
    :: 去除前后空格
    for /f "tokens=*" %%a in ("!new_package_name!") do set "new_package_name=%%a"
    call :updatecfg PACKAGE "!new_package_name!"
    echo 游戏包名已修改为：%GREEN%!new_package_name!%RESET%
)
echo.
goto settingmenu

:: 修改备份根目录

:modifyroot
echo 当前备份根目录：%GREEN%!BACKUP_ROOT!%RESET%
set "new_backup_root="
set /p new_backup_root=请输入新的备份根目录（直接回车保持当前设置）：
if not defined new_backup_root (
    echo 备份根目录未修改，仍为：%GREEN%!BACKUP_ROOT!%RESET%
) else (
    :: 去除前后空格
    for /f "tokens=*" %%a in ("!new_backup_root!") do set "new_backup_root=%%a"
    call :updatecfg BACKUP_ROOT "!new_backup_root!"
    echo 备份根目录已修改为：%GREEN%!new_backup_root!%RESET%
)
echo.
goto settingmenu

:: 修改存档ID

:modifyuid
echo 当前存档ID：%GREEN%!SAVE_ID!%RESET%
set "new_save_id="
set /p new_save_id=请输入新的存档ID（直接回车保持当前设置）：

if not defined new_save_id (
    echo 存档ID未修改，仍为：%GREEN%!SAVE_ID!%RESET%
) else (
    :: 去除前后空格
    for /f "tokens=*" %%a in ("!new_save_id!") do set "new_save_id=%%a"
    call :updatecfg SAVE_ID "!new_save_id!"
    echo 存档ID已修改为：%GREEN%!new_save_id!%RESET%
)
echo.
goto settingmenu

:: 初始化配置文件

:initcfg
echo @echo off > config.bat
echo set "ADB=ADB Files\adb.exe" >> config.bat
echo set "DEVICE=" >> config.bat
echo set "PACKAGE=com.ChillyRoom.DungeonShooter" >> config.bat
echo set "BACKUP_ROOT=" >> config.bat
echo set "SAVE_ID=" >> config.bat
exit /b

:: 更新配置文件

:updatecfg
set "CONFIG_KEY=%~1"
set "CONFIG_VALUE=%~2"
if not exist "config.bat" (
    call :initcfg
)
:: 读取现有配置（重新读取配置文件以获取最新值）
setlocal EnableDelayedExpansion
call config.bat
set "NEW_ADB=!ADB!"
set "NEW_DEVICE=!DEVICE!"
set "NEW_PACKAGE=!PACKAGE!"
set "NEW_BACKUP_ROOT=!BACKUP_ROOT!"
set "NEW_SAVE_ID=!SAVE_ID!"
:: 更新对应的配置项
if /i "!CONFIG_KEY!"=="ADB" set "NEW_ADB=!CONFIG_VALUE!"
if /i "!CONFIG_KEY!"=="DEVICE" set "NEW_DEVICE=!CONFIG_VALUE!"
if /i "!CONFIG_KEY!"=="PACKAGE" set "NEW_PACKAGE=!CONFIG_VALUE!"
if /i "!CONFIG_KEY!"=="BACKUP_ROOT" set "NEW_BACKUP_ROOT=!CONFIG_VALUE!"
if /i "!CONFIG_KEY!"=="SAVE_ID" set "NEW_SAVE_ID=!CONFIG_VALUE!"
:: 写入新配置
echo @echo off > config.bat
echo set "ADB=!NEW_ADB!" >> config.bat
echo set "DEVICE=!NEW_DEVICE!" >> config.bat
echo set "PACKAGE=!NEW_PACKAGE!" >> config.bat
echo set "BACKUP_ROOT=!NEW_BACKUP_ROOT!" >> config.bat
echo set "SAVE_ID=!NEW_SAVE_ID!" >> config.bat
endlocal
:: 重新读取配置文件以更新主作用域的变量
call config.bat
exit /b

:backup
call :chk

:: 生成时间戳目录
for /f "tokens=2 delims==" %%a in ('wmic os get localdatetime /value') do set DT=%%a
set "TS=%DT:~0,8%_%DT:~8,6%"
set "BACKUP_DIR=%BACKUP_ROOT%\backup_%TS%"
md "%BACKUP_DIR%" 2>nul

:: 循环处理每个文件
set "FILE_INDEX=0"
set "SUCCESS_COUNT=0"
set "FAIL_COUNT=0"

:: 检测SAVE_ID是否为空
set "NEED_BACKUP_ALL=0"
if not defined SAVE_ID (
    set "NEED_BACKUP_ALL=1"
) else (
    :: 检查是否为空或默认提示文本
    if "!SAVE_ID!"=="" set "NEED_BACKUP_ALL=1"
    if "!SAVE_ID!"=="请输入存档ID" set "NEED_BACKUP_ALL=1"
)

:: 根据SAVE_ID是否为空选择备份方式
if "!NEED_BACKUP_ALL!"=="1" (
    :: SAVE_ID为空，备份整个files文件夹
    call :backupfiles
) else (
    :: SAVE_ID已设置，备份指定文件
    for %%F in (
        "battles_!SAVE_ID!_.data"
        "item_data_!SAVE_ID!_.data"
        "item_data_!SAVE_ID!_.data.new"
        "item_data.data"
        "mall_reload_data_!SAVE_ID!_.data"
        "mall_reload_data_!SAVE_ID!_.data.new"
        "monsrise_data_!SAVE_ID!_.data"
        "monsrise_data_!SAVE_ID!_.data.new"
        "sandbox_config_!SAVE_ID!_.data"
        "sandbox_maps_!SAVE_ID!_.data"
        "season_data_!SAVE_ID!_.data"
        "season_data_!SAVE_ID!_.data.new"
        "season_data.data"
        "statistic_!SAVE_ID!_.data"
        "statistic_!SAVE_ID!_.data.new"
        "statistic.data"
        "task_!SAVE_ID!_.data"
        "task_!SAVE_ID!_.data.new"
        "weapon_evolution_data_!SAVE_ID!_.data"
        "weapon_evolution_data_!SAVE_ID!_.data.new"
    ) do (
        call :backupfile "files/%%~F" "files" "%%~F"
    )
)

:: 备份shared_prefs目录下的文件
call :backupfile "shared_prefs/%PACKAGE%.v2.playerprefs.xml" "shared_prefs" "%PACKAGE%.v2.playerprefs.xml"
goto :backupdone

:: 处理files目录下的所有子文件
:backupfiles
:: 创建目标文件夹
set "TARGET_DIR=!BACKUP_DIR!\files"
md "!TARGET_DIR!" 2>nul

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

:backupfile
set "SOURCE_PATH=%~1"
set "TARGET_FOLDER=%~2"
set "TARGET_NAME=%~3"

:: 创建目标文件夹
set "TARGET_DIR=!BACKUP_DIR!\!TARGET_FOLDER!"
md "!TARGET_DIR!" 2>nul

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

:backupdone

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
call :chk

:: 让用户输入备份文件夹名称
set /p "FOLDER_NAME=请输入要还原文件所在文件夹的名称："
echo.
set "SELECTED=!BACKUP_ROOT!\!FOLDER_NAME!"
if not exist "!SELECTED!" (
    echo %RED%备份文件夹不存在：!SELECTED!%RESET%
    pause & echo.
    goto menu
)

:: 循环处理每个文件
set "FILE_INDEX=0"
set "SUCCESS_COUNT=0"
set "FAIL_COUNT=0"
set "SKIP_COUNT=0"
:: 处理files目录下的所有文件
call :restorefiles
call :restorefile "shared_prefs/%PACKAGE%.v2.playerprefs.xml" "shared_prefs" "%PACKAGE%.v2.playerprefs.xml"
goto :restoredone

:: 处理files目录下的所有文件

:restorefiles
set "FILES_SOURCE_DIR=!SELECTED!\files"
if not exist "!FILES_SOURCE_DIR!" (
    echo %YELLOW%备份目录中不存在files文件夹，跳过files目录还原%RESET%
    exit /b
)

:: 扫描files文件夹下的所有文件
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

:restorefile
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

:restoredone

echo %GREEN%还原完成！%RESET%
echo 成功：%SUCCESS_COUNT% 个文件，失败：%FAIL_COUNT% 个文件，跳过：%SKIP_COUNT% 个文件
echo.
goto menu

:end
echo 已退出。
exit /b

:: 检查adb连接
:chk
"%ADB%" devices | findstr /b /c:"!DEVICE!" >nul
if errorlevel 1 (
    echo %RED%设备 !DEVICE! 未连接%RESET%
    echo.
    pause &     echo.
    goto menu
)
exit /b
