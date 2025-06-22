// FileCopy.cpp
#include "pch.h"
#define FILECOPY_API __declspec(dllexport)

#include <windows.h>
#include <string>
#include <filesystem>
#include <vector>
#include <sstream>

// 声明CopyDirectory函数
static void CopyDirectory(const std::string& sourceDir, const std::string& targetDir);

extern "C" FILECOPY_API void CopyFiles(const char* sourcePaths, const char* targetPath, const char* style, bool cleanTargetFolder)
{
    try
    {
        std::string sourcePathsStr = sourcePaths; // 保存源路径
        std::string targetPathStr = targetPath; // 保存目标路径
        std::string styleStr = style; // 保存复制方式

        // 如果需要删除目标文件夹，则先删除
        if (cleanTargetFolder)
        {
            std::filesystem::remove_all(targetPathStr);
            std::filesystem::create_directories(targetPathStr); // 重新创建空文件夹
        }

        if (styleStr == "File")
        {
            std::vector<std::string> files; // 保存文件路径
            std::istringstream stream(sourcePathsStr); // 读取源路径
            std::string file; // 保存文件路径

            while (std::getline(stream, file, '\n'))
            {
                if (!file.empty() && std::filesystem::exists(file))
                {
                    std::string fileName = std::filesystem::path(file).filename().string(); // 获取文件名
                    std::string destFile = targetPathStr + "\\" + fileName; // 计算目标文件路径
                    CopyFileA(file.c_str(), destFile.c_str(), TRUE); // 复制文件
                }
            }
        }
        else if (styleStr == "Folder")
        {
            std::istringstream stream(sourcePathsStr); // 读取源路径
            std::string sourceFolder; // 保存源文件夹路径

            while (std::getline(stream, sourceFolder, '\n'))
            {
                if (!sourceFolder.empty() && std::filesystem::exists(sourceFolder))
                {
                    std::string targetFolder = targetPathStr + "\\" + std::filesystem::path(sourceFolder).filename().string(); // 计算目标文件夹路径
                    CopyDirectory(sourceFolder, targetFolder); // 复制文件夹
                }
            }
        }
    }
    catch (const std::exception& e)
    {
        MessageBoxA(NULL, e.what(), "错误", MB_ICONERROR); // 弹出错误提示
    }
}

// 定义CopyDirectory函数
static void CopyDirectory(const std::string& sourceDir, const std::string& targetDir)
{
    std::filesystem::create_directories(targetDir); // 创建目标文件夹

    for (const auto& entry : std::filesystem::recursive_directory_iterator(sourceDir))
    {
        const std::filesystem::path& path = entry.path(); // 获取文件或文件夹路径
        std::string targetPath = targetDir + path.generic_string().substr(sourceDir.length()); // 计算目标路径

        if (std::filesystem::is_directory(path))
        {
            std::filesystem::create_directories(targetPath); // 创建子文件夹
        }
        else
        {
            CopyFileA(path.string().c_str(), targetPath.c_str(), TRUE); // 复制文件
        }
    }
}