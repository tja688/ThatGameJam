using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Xml;

public class USSBatchLinker : EditorWindow
{
    private string uxmlFolderPath = "Assets/UI/UXML";
    private string ussFolderPath = "Assets/UI/USS";

    [MenuItem("Tools/UI Toolkit/USS 批量绑定工具")]
    public static void ShowWindow()
    {
        GetWindow<USSBatchLinker>("USS 批量绑定");
    }

    private void OnGUI()
    {
        GUILayout.Label("批量将 USS 样式表关联到 UXML 文件", EditorStyles.boldLabel);

        uxmlFolderPath = EditorGUILayout.TextField("UXML 文件夹路径", uxmlFolderPath);
        ussFolderPath = EditorGUILayout.TextField("USS 文件夹路径", ussFolderPath);

        if (GUILayout.Button("开始执行批量绑定"))
        {
            ProcessBinding();
        }
    }

    private void ProcessBinding()
    {
        // 获取所有 USS 文件的 GUID 路径
        string[] ussFiles = Directory.GetFiles(ussFolderPath, "*.uss", SearchOption.AllDirectories);
        if (ussFiles.Length == 0)
        {
            Debug.LogError("未在指定路径找到 USS 文件！");
            return;
        }

        // 获取所有 UXML 文件
        string[] uxmlFiles = Directory.GetFiles(uxmlFolderPath, "*.uxml", SearchOption.AllDirectories);
        
        int count = 0;
        foreach (string uxmlPath in uxmlFiles)
        {
            if (ApplyStylesToUxml(uxmlPath, ussFiles))
            {
                count++;
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"已处理 {count} 个 UXML 文件。", "确定");
    }

    private bool ApplyStylesToUxml(string uxmlPath, string[] ussFiles)
    {
        XmlDocument xmlDoc = new XmlDocument();
        try
        {
            xmlDoc.Load(uxmlPath);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            // UI Toolkit 的默认命名空间
            nsmgr.AddNamespace("ui", "UnityEngine.UIElements");

            XmlNode root = xmlDoc.DocumentElement;
            if (root == null) return false;

            bool modified = false;

            foreach (string ussFilePath in ussFiles)
            {
                // 将系统路径转换为 AssetDatabase 路径
                string assetPath = ussFilePath.Replace("\\", "/");
                if (assetPath.StartsWith(Application.dataPath))
                {
                    assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                }
                else if (assetPath.Contains("Assets/"))
                {
                    assetPath = assetPath.Substring(assetPath.IndexOf("Assets/"));
                }

                string projectUri = $"project://database/{assetPath}";

                // 检查是否已经存在该样式的引用，避免重复添加
                bool exists = false;
                foreach (XmlNode child in root.ChildNodes)
                {
                    if (child.Name == "Style" || child.Name == "ui:Style")
                    {
                        if (child.Attributes["src"]?.Value == assetPath ||
                            child.Attributes["src"]?.Value == projectUri ||
                            child.Attributes["path"]?.Value == assetPath)
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                if (!exists)
                {
                    // 创建新的 Style 节点
                    XmlElement styleElement = xmlDoc.CreateElement("ui", "Style", "UnityEngine.UIElements");
                    // 也可以直接用 src 属性指向文件
                    styleElement.SetAttribute("src", projectUri);
                    
                    // 将 Style 节点插入到根节点的第一个位置（规范做法）
                    root.PrependChild(styleElement);
                    modified = true;
                }
            }

            if (modified)
            {
                xmlDoc.Save(uxmlPath);
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"处理文件 {uxmlPath} 时出错: {e.Message}");
        }

        return false;
    }
}
