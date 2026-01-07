using System;
using System.IO;
using UnityEngine;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// 路径处理工具类
    /// </summary>
    public static class PathUtil
    {
        /// <summary>
        /// 标准化 Unity Asset 路径（使用正斜杠，去除尾部斜杠）
        /// </summary>
        public static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            return path.Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>
        /// 检查路径是否在 Assets 目录下
        /// </summary>
        public static bool IsUnderAssets(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            var normalized = NormalizeAssetPath(assetPath);
            return normalized.StartsWith("Assets/") || normalized == "Assets";
        }

        /// <summary>
        /// 检查路径是否在项目目录内（防止越权写入）
        /// </summary>
        public static bool IsWithinProject(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return false;
            
            try
            {
                var fullPath = Path.GetFullPath(absolutePath);
                var projectRoot = Path.GetFullPath(AutoGenSettings.ProjectRoot);
                return fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将相对于项目根目录的路径转换为绝对路径
        /// </summary>
        public static string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return relativePath;
            if (Path.IsPathRooted(relativePath)) return relativePath;
            return Path.Combine(AutoGenSettings.ProjectRoot, relativePath);
        }

        /// <summary>
        /// 将绝对路径转换为相对于项目根目录的路径
        /// </summary>
        public static string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return absolutePath;
            
            try
            {
                var fullPath = Path.GetFullPath(absolutePath);
                var projectRoot = Path.GetFullPath(AutoGenSettings.ProjectRoot);
                
                if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var relative = fullPath.Substring(projectRoot.Length);
                    return relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                return absolutePath;
            }
            catch
            {
                return absolutePath;
            }
        }

        /// <summary>
        /// 从 Asset 路径转换为绝对文件系统路径
        /// </summary>
        public static string AssetPathToAbsolute(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return assetPath;
            return Path.Combine(AutoGenSettings.ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// 从绝对路径转换为 Asset 路径
        /// </summary>
        public static string AbsoluteToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return absolutePath;
            
            try
            {
                var fullPath = Path.GetFullPath(absolutePath);
                var projectRoot = Path.GetFullPath(AutoGenSettings.ProjectRoot);
                
                if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var relative = fullPath.Substring(projectRoot.Length);
                    relative = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    return NormalizeAssetPath(relative);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        public static void EnsureDirectoryExists(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// 安全移动文件（同卷原子移动优先）
        /// </summary>
        public static bool SafeMoveFile(string source, string dest)
        {
            try
            {
                EnsureDirectoryExists(dest);
                
                // 如果目标已存在，先删除
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }
                
                File.Move(source, dest);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AutoGenJobs] Failed to move file from {source} to {dest}: {e.Message}");
                return false;
            }
        }
    }
}
