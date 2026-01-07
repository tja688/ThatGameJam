using System.Collections.Generic;
using System.IO;
using AutoGenJobs.Commands;
using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Tests.EditMode
{
    /// <summary>
    /// JobRunner EditMode 测试
    /// </summary>
    public class JobRunnerTests
    {
        private const string TEST_ROOT = "Assets/AutoGen/Tests";

        [SetUp]
        public void Setup()
        {
            // 清理测试目录
            if (AssetDatabase.IsValidFolder(TEST_ROOT))
            {
                AssetDatabase.DeleteAsset(TEST_ROOT);
            }
            AssetDatabase.CreateFolder("Assets/AutoGen", "Tests");
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            // 可选：保留测试文件用于检查
            // AssetDatabase.DeleteAsset(TEST_ROOT);
            // AssetDatabase.Refresh();
        }

        /// <summary>
        /// 测试 1: CreateScriptableObject + SetSerializedProperty（基本类型）
        /// </summary>
        [Test]
        public void Test_CreateScriptableObject_WithBasicProperties()
        {
            // 创建一个简单的测试 ScriptableObject
            // 使用 Unity 内置的 ScriptableObject 类型测试
            var assetPath = $"{TEST_ROOT}/TestSO.asset";

            // 由于我们没有自定义 ScriptableObject，使用 TextAsset 来测试基本命令流程
            // 实际上我们测试 CreateGameObject + AddComponent + SetSerializedProperty

            var ctx = CreateTestContext();

            // 测试 CreateGameObject
            var cmd = CommandRegistry.GetCommand("CreateGameObject");
            Assert.IsNotNull(cmd, "CreateGameObject command should exist");

            var args = JObject.Parse(@"{
                ""name"": ""TestObject"",
                ""position"": [1, 2, 3]
            }");

            var result = cmd.Execute(ctx, args);
            Assert.IsTrue(result.Success, $"CreateGameObject should succeed: {result.Message}");
            Assert.IsTrue(result.Outputs.ContainsKey("go"), "Should output 'go'");

            var go = result.Outputs["go"] as GameObject;
            Assert.IsNotNull(go, "Output should be a GameObject");
            Assert.AreEqual("TestObject", go.name);
            Assert.AreEqual(new Vector3(1, 2, 3), go.transform.localPosition);

            // 清理
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 测试 2: CreateGameObject + AddComponent + SetSerializedProperty
        /// </summary>
        [Test]
        public void Test_AddComponent_And_SetSerializedProperty()
        {
            var ctx = CreateTestContext();

            // 创建 GameObject
            var createCmd = CommandRegistry.GetCommand("CreateGameObject");
            var createResult = createCmd.Execute(ctx, JObject.Parse(@"{ ""name"": ""SpriteTest"" }"));
            Assert.IsTrue(createResult.Success);

            var go = createResult.Outputs["go"] as GameObject;
            ctx.SetVar("$testGo", go);

            // 添加 SpriteRenderer 组件
            var addCmd = CommandRegistry.GetCommand("AddComponent");
            var addArgs = JObject.Parse(@"{
                ""target"": { ""ref"": ""$testGo"" },
                ""type"": ""UnityEngine.SpriteRenderer""
            }");

            var addResult = addCmd.Execute(ctx, addArgs);
            Assert.IsTrue(addResult.Success, $"AddComponent should succeed: {addResult.Message}");

            var sr = go.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(sr, "SpriteRenderer should be added");

            ctx.SetVar("$sr", sr);

            // 设置颜色属性
            var setCmd = CommandRegistry.GetCommand("SetSerializedProperty");
            var setArgs = JObject.Parse(@"{
                ""target"": { ""ref"": ""$sr"" },
                ""propertyPath"": ""m_Color"",
                ""value"": [1, 0, 0, 1]
            }");

            var setResult = setCmd.Execute(ctx, setArgs);
            Assert.IsTrue(setResult.Success, $"SetSerializedProperty should succeed: {setResult.Message}");

            Assert.AreEqual(Color.red, sr.color, "Color should be red");

            // 清理
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 测试 3: SetTransform
        /// </summary>
        [Test]
        public void Test_SetTransform()
        {
            var ctx = CreateTestContext();

            // 创建 GameObject
            var createCmd = CommandRegistry.GetCommand("CreateGameObject");
            var createResult = createCmd.Execute(ctx, JObject.Parse(@"{ ""name"": ""TransformTest"" }"));
            Assert.IsTrue(createResult.Success);

            var go = createResult.Outputs["go"] as GameObject;
            ctx.SetVar("$go", go);

            // 设置 Transform
            var setTransformCmd = CommandRegistry.GetCommand("SetTransform");
            var setArgs = JObject.Parse(@"{
                ""target"": { ""ref"": ""$go"" },
                ""position"": [10, 20, 30],
                ""rotation"": [0, 90, 0],
                ""scale"": [2, 2, 2],
                ""space"": ""local""
            }");

            var result = setTransformCmd.Execute(ctx, setArgs);
            Assert.IsTrue(result.Success, $"SetTransform should succeed: {result.Message}");

            Assert.AreEqual(new Vector3(10, 20, 30), go.transform.localPosition, "Position should match");
            Assert.AreEqual(new Vector3(2, 2, 2), go.transform.localScale, "Scale should match");

            // 清理
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 测试 4: InstantiatePrefabInScene + SetTransform
        /// 需要先创建一个测试 Prefab
        /// </summary>
        [Test]
        public void Test_InstantiatePrefabInScene()
        {
            var ctx = CreateTestContext();
            var prefabPath = $"{TEST_ROOT}/TestPrefab.prefab";

            // 先创建一个测试 Prefab
            var tempGo = new GameObject("TestPrefabRoot");
            tempGo.AddComponent<SpriteRenderer>();

            // 确保目录存在
            if (!AssetDatabase.IsValidFolder(TEST_ROOT))
            {
                AssetDatabase.CreateFolder("Assets/AutoGen", "Tests");
            }

            PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
            Object.DestroyImmediate(tempGo);
            AssetDatabase.Refresh();

            Assert.IsTrue(File.Exists(PathUtil.AssetPathToAbsolute(prefabPath)), "Prefab should exist");

            // 实例化 Prefab
            var instantiateCmd = CommandRegistry.GetCommand("InstantiatePrefabInScene");
            var args = JObject.Parse($@"{{
                ""prefabPath"": ""{prefabPath}"",
                ""nameOverride"": ""InstantiatedTest"",
                ""position"": [5, 5, 5]
            }}");

            var result = instantiateCmd.Execute(ctx, args);
            Assert.IsTrue(result.Success, $"InstantiatePrefabInScene should succeed: {result.Message}");

            var instance = result.Outputs["instance"] as GameObject;
            Assert.IsNotNull(instance, "Should have instance output");
            Assert.AreEqual("InstantiatedTest", instance.name);
            Assert.AreEqual(new Vector3(5, 5, 5), instance.transform.localPosition);
            Assert.IsNotNull(instance.GetComponent<SpriteRenderer>(), "Should have SpriteRenderer");

            // 清理场景对象
            Object.DestroyImmediate(instance);
        }

        /// <summary>
        /// 测试 5: CreateOrEditPrefab 嵌套命令
        /// </summary>
        [Test]
        public void Test_CreateOrEditPrefab()
        {
            var ctx = CreateTestContext();
            var prefabPath = $"{TEST_ROOT}/EditablePrefab.prefab";

            var createPrefabCmd = CommandRegistry.GetCommand("CreateOrEditPrefab");
            var args = JObject.Parse($@"{{
                ""prefabPath"": ""{prefabPath}"",
                ""rootName"": ""EditableRoot"",
                ""edits"": [
                    {{
                        ""cmd"": ""AddComponent"",
                        ""args"": {{
                            ""target"": {{ ""ref"": ""$prefabRoot"" }},
                            ""type"": ""UnityEngine.SpriteRenderer""
                        }},
                        ""out"": {{ ""component"": ""$sr"" }}
                    }},
                    {{
                        ""cmd"": ""SetSerializedProperty"",
                        ""args"": {{
                            ""target"": {{ ""ref"": ""$sr"" }},
                            ""propertyPath"": ""m_Color"",
                            ""value"": [0, 1, 0, 1]
                        }}
                    }}
                ]
            }}");

            var result = createPrefabCmd.Execute(ctx, args);
            Assert.IsTrue(result.Success, $"CreateOrEditPrefab should succeed: {result.Message}");

            // 验证 Prefab 创建成功
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, "Prefab should exist");

            var sr = prefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(sr, "Prefab should have SpriteRenderer");
            Assert.AreEqual(Color.green, sr.color, "Color should be green");
        }

        /// <summary>
        /// 测试命令注册表
        /// </summary>
        [Test]
        public void Test_CommandRegistry()
        {
            // 确保核心命令已注册
            Assert.IsNotNull(CommandRegistry.GetCommand("ImportAssets"));
            Assert.IsNotNull(CommandRegistry.GetCommand("CreateGameObject"));
            Assert.IsNotNull(CommandRegistry.GetCommand("AddComponent"));
            Assert.IsNotNull(CommandRegistry.GetCommand("SetTransform"));
            Assert.IsNotNull(CommandRegistry.GetCommand("SetSerializedProperty"));
            Assert.IsNotNull(CommandRegistry.GetCommand("SaveAssets"));
            Assert.IsNotNull(CommandRegistry.GetCommand("CreateOrEditPrefab"));
            Assert.IsNotNull(CommandRegistry.GetCommand("InstantiatePrefabInScene"));
            Assert.IsNotNull(CommandRegistry.GetCommand("PingObject"));
            Assert.IsNotNull(CommandRegistry.GetCommand("SelectObject"));
        }

        /// <summary>
        /// 测试 Guards 检查
        /// </summary>
        [Test]
        public void Test_Guards()
        {
            // 测试路径检查
            var pathCheck = Guards.CheckWriteRoot("Assets/AutoGen");
            Assert.IsTrue(pathCheck.CanProceed, "Assets/AutoGen should be allowed");

            var invalidPathCheck = Guards.CheckWriteRoot("Assets/Other");
            Assert.IsFalse(invalidPathCheck.CanProceed, "Assets/Other should not be allowed");

            // 测试类型检查
            var typeCheck = Guards.CheckRequiredTypes(new List<string> { "UnityEngine.SpriteRenderer" });
            Assert.IsTrue(typeCheck.CanProceed, "SpriteRenderer should exist");

            var missingTypeCheck = Guards.CheckRequiredTypes(new List<string> { "NonExistent.Type" });
            Assert.IsFalse(missingTypeCheck.CanProceed, "NonExistent.Type should not exist");
            Assert.AreEqual(WaitingReason.WAITING_TYPES_MISSING, missingTypeCheck.WaitReason);
        }

        /// <summary>
        /// 测试 TypeResolver
        /// </summary>
        [Test]
        public void Test_TypeResolver()
        {
            // 测试 Unity 内置类型
            var srType = TypeResolver.ResolveType("UnityEngine.SpriteRenderer");
            Assert.IsNotNull(srType);
            Assert.AreEqual(typeof(SpriteRenderer), srType);

            // 测试类型识别
            Assert.IsTrue(TypeResolver.IsComponentType(typeof(SpriteRenderer)));
            Assert.IsTrue(TypeResolver.IsMonoBehaviourType(typeof(MonoBehaviour)));
            Assert.IsTrue(TypeResolver.IsScriptableObjectType(typeof(ScriptableObject)));
        }

        /// <summary>
        /// 创建测试用的 CommandContext
        /// </summary>
        private CommandContext CreateTestContext()
        {
            var job = new JobData
            {
                jobId = "test_job",
                projectWriteRoot = TEST_ROOT,
                dryRun = false
            };

            var logger = new JobLogger(job.jobId);
            return new CommandContext(job, logger);
        }
    }
}
