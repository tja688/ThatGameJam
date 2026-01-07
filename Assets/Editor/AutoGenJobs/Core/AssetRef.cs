using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// Asset 引用解析工具类
    /// 统一使用 GUID 解析 Unity Asset
    /// </summary>
    public static class AssetRef
    {
        /// <summary>
        /// 从 GUID 加载 Asset
        /// </summary>
        public static T LoadByGuid<T>(string guid) where T : Object
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                AutoGenLog.Warning($"Cannot find asset path for GUID: {guid}");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>
        /// 从路径加载 Asset
        /// </summary>
        public static T LoadByPath<T>(string assetPath) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        /// <summary>
        /// 从 GUID 或路径加载 Asset（优先 GUID）
        /// </summary>
        public static T Load<T>(string guid, string path) where T : Object
        {
            if (!string.IsNullOrEmpty(guid))
            {
                var asset = LoadByGuid<T>(guid);
                if (asset != null)
                    return asset;
            }

            if (!string.IsNullOrEmpty(path))
            {
                return LoadByPath<T>(path);
            }

            return null;
        }

        /// <summary>
        /// 从路径获取 GUID
        /// </summary>
        public static string GetGuid(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        /// <summary>
        /// 从 GUID 获取路径
        /// </summary>
        public static string GetPath(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            return AssetDatabase.GUIDToAssetPath(guid);
        }

        /// <summary>
        /// 检查 Asset 是否存在
        /// </summary>
        public static bool Exists(string guid, string path = null)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                var resolvedPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(resolvedPath))
                    return true;
            }

            if (!string.IsNullOrEmpty(path))
            {
                return AssetDatabase.LoadAssetAtPath<Object>(path) != null;
            }

            return false;
        }

        /// <summary>
        /// 加载主资产和子资产（如 Sprite from Texture）
        /// </summary>
        public static T LoadSubAsset<T>(string assetPath, string subAssetName = null) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            // 如果没有指定子资产名，尝试加载主资产
            if (string.IsNullOrEmpty(subAssetName))
            {
                // 特殊处理 Sprite：如果目标类型是 Sprite，尝试从 Texture 中加载
                if (typeof(T) == typeof(Sprite))
                {
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    foreach (var obj in sprites)
                    {
                        if (obj is T t)
                            return t;
                    }
                }

                return AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }

            // 加载指定名称的子资产
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in allAssets)
            {
                if (obj is T t && obj.name == subAssetName)
                    return t;
            }

            return null;
        }

        /// <summary>
        /// 创建 Asset 并保存
        /// </summary>
        public static void CreateAsset(Object asset, string assetPath, bool overwrite = false)
        {
            if (asset == null || string.IsNullOrEmpty(assetPath))
            {
                AutoGenLog.Error("Cannot create asset: null asset or empty path");
                return;
            }

            // 确保目录存在
            var directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                var fullDir = PathUtil.AssetPathToAbsolute(directory);
                if (!System.IO.Directory.Exists(fullDir))
                {
                    System.IO.Directory.CreateDirectory(fullDir);
                    AssetDatabase.Refresh();
                }
            }

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (existing != null)
            {
                if (!overwrite)
                {
                    AutoGenLog.Warning($"Asset already exists at {assetPath}, not overwriting");
                    return;
                }
                else
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
        }
    }
}
