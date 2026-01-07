using System.IO;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Setup
{
    /// <summary>
    /// AutoGen Job System 工作区初始化
    /// </summary>
    public static class WorkspaceInitializer
    {
        private const string MENU_PATH = "Tools/AutoGen Jobs/Initialize Workspace";

        /// <summary>
        /// 初始化工作区目录结构
        /// </summary>
        [MenuItem(MENU_PATH, priority = 50)]
        public static void InitializeWorkspace()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            
            // 1. 创建 AutoGenJobs 目录结构
            var jobsRoot = Path.Combine(projectRoot, "AutoGenJobs");
            var directories = new[] { "inbox", "working", "done", "results", "dead", "examples" };
            
            foreach (var dir in directories)
            {
                var path = Path.Combine(jobsRoot, dir);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Debug.Log($"[AutoGen] Created: {path}");
                }
            }

            // 2. 创建 Assets/AutoGen 目录结构
            if (!AssetDatabase.IsValidFolder("Assets/AutoGen"))
            {
                AssetDatabase.CreateFolder("Assets", "AutoGen");
            }
            if (!AssetDatabase.IsValidFolder("Assets/AutoGen/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/AutoGen", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder("Assets/AutoGen/Configs"))
            {
                AssetDatabase.CreateFolder("Assets/AutoGen", "Configs");
            }

            // 3. 创建 .agent/skills 目录（如果需要）
            var skillsPath = Path.Combine(projectRoot, ".agent", "skills");
            if (!Directory.Exists(skillsPath))
            {
                Directory.CreateDirectory(skillsPath);
                Debug.Log($"[AutoGen] Created: {skillsPath}");
                
                // 提示用户复制 Skills
                EditorUtility.DisplayDialog(
                    "Skills 目录已创建",
                    "请从 Package Samples 中导入 Skills 文件到 .agent/skills/ 目录。\n\n" +
                    "路径: Window > Package Manager > AutoGen Job System > Samples > Skills",
                    "OK"
                );
            }

            // 4. 创建示例 Job 文件
            CreateExampleJob(jobsRoot);

            // 5. 刷新
            AssetDatabase.Refresh();

            Debug.Log("[AutoGen] Workspace initialized successfully!");
            EditorUtility.DisplayDialog(
                "AutoGen 工作区初始化完成",
                "目录结构已创建：\n\n" +
                "• AutoGenJobs/ (Job 文件系统)\n" +
                "• Assets/AutoGen/ (生成资产目录)\n" +
                "• .agent/skills/ (Agent Skills)\n\n" +
                "Runner 已启动，可以开始使用了！",
                "OK"
            );
        }

        private static void CreateExampleJob(string jobsRoot)
        {
            var examplePath = Path.Combine(jobsRoot, "examples", "example_hello.job.json");
            if (!File.Exists(examplePath))
            {
                var exampleContent = @"{
  ""schemaVersion"": 1,
  ""jobId"": ""example_hello"",
  ""projectWriteRoot"": ""Assets/AutoGen"",
  ""commands"": [
    {
      ""cmd"": ""CreateGameObject"",
      ""args"": {
        ""name"": ""HelloAutoGen"",
        ""position"": [0, 1, 0],
        ""ensure"": true
      },
      ""out"": { ""go"": ""$hello"" }
    },
    {
      ""cmd"": ""AddComponent"",
      ""args"": {
        ""target"": { ""ref"": ""$hello"" },
        ""type"": ""UnityEngine.SpriteRenderer""
      }
    }
  ],
  ""meta"": {
    ""note"": ""将此文件复制到 inbox/ 目录来测试""
  }
}";
                File.WriteAllText(examplePath, exampleContent);
                Debug.Log($"[AutoGen] Created example: {examplePath}");
            }
        }

        /// <summary>
        /// 安装 Skills 到项目
        /// </summary>
        [MenuItem("Tools/AutoGen Jobs/Install Skills", priority = 51)]
        public static void InstallSkills()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var targetPath = Path.Combine(projectRoot, ".agent", "skills");
            
            // 查找 Package 中的 Skills
            var packagePath = Path.Combine(projectRoot, "Packages", "com.autogen.jobs", "Samples~", "Skills");
            
            if (!Directory.Exists(packagePath))
            {
                // 尝试查找 Library 中的缓存
                var guids = AssetDatabase.FindAssets("unity-autogen-jobs", new[] { "Packages" });
                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog(
                        "Skills 未找到",
                        "无法找到 Skills 文件。请确保 AutoGen Job System 包已正确安装。",
                        "OK"
                    );
                    return;
                }
            }

            Directory.CreateDirectory(targetPath);

            var skillFiles = new[] 
            { 
                "_index.md",
                "unity-autogen-jobs.md", 
                "unity-autogen-templates.md", 
                "unity-autogen-executor.md" 
            };

            int copied = 0;
            foreach (var file in skillFiles)
            {
                var src = Path.Combine(packagePath, file);
                var dst = Path.Combine(targetPath, file);
                
                if (File.Exists(src))
                {
                    File.Copy(src, dst, true);
                    copied++;
                }
            }

            if (copied > 0)
            {
                EditorUtility.DisplayDialog(
                    "Skills 已安装",
                    $"已复制 {copied} 个 Skill 文件到:\n{targetPath}",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "未找到 Skills",
                    $"请手动从 Package Samples 复制 Skills 文件。\n\n源路径: {packagePath}",
                    "OK"
                );
            }
        }
    }
}
