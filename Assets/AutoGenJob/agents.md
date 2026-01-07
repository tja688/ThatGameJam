# AutoGen Unity Job System — Agent.md

## 0. 你要实现的东西是什么（目标与边界）

### 目标（必须满足）
你要在 Unity Editor 进程“保持打开、可交互”的情况下，实现一个常驻的 Job Runner：
- 外部工具/本地 AI 通过写入 Job 文件（JSON）来请求 Unity 执行自动化操作。
- Unity 侧接收 Job，按 `commands[]` 逐步执行：
  - 导入/刷新图片、音频等资源
  - 创建 ScriptableObject（SO）资产，并给字段赋值
  - 创建或编辑 Prefab（添加组件、设置字段、挂引用）
  - 在“当前打开的 Unity 场景（Active Scene）”中创建对象/实例化 Prefab/挂组件/设置 transform
- Runner 必须在 Unity 失去焦点时也能工作（只要 Unity 没被暂停且 Editor 在运行）。
- 不允许为了执行 Job 而关闭 Unity（不使用 batchmode / gunchmode 这类单实例冲突方案）。

### 非目标（不要做/不急着做）
- 不做运行时（Play Mode 运行中）的自动化（只做 Edit Mode）。
- 不做复杂的“代码生成 -> 编译 -> 立刻使用新类型”的频繁循环（必须可支持，但要有明确的依赖门槛与 WAITING 状态，避免旧域抢跑报错）。

---

## 1. 关键设计原则（你必须遵守）

### 1.1 Runner 代码稳定，能力用数据表达
旧系统问题：每新增一种“操作类型”，就要改 Runner 的路由逻辑，导致编译竞态 + 史山增长。
新系统：Job 永远是固定协议（少量固定 jobType），真正的变化通过 `commands[]` 描述，Runner 只实现一组“原子命令”。

### 1.2 解决编译/域重载竞态：显式依赖门槛
Job 必须支持：
- `runnerMinVersion`: Runner 版本不足 -> 不报错，不消费，返回 WAITING。
- `requiresTypes`: 依赖的 C# 类型不存在（未编译完成）-> WAITING。
- Runner 必须在 `EditorApplication.isCompiling` / `isUpdating` / domain reload 期间停止消费新的 job。

### 1.3 资源引用统一用 GUID（不是路径）
Job 中引用 Unity Asset 一律用 GUID（或 GUID + fallback path）：
- 避免资源移动/改名导致 path 失效。
- Runner 使用 `AssetDatabase.GUIDToAssetPath()` 解析，再 `LoadAssetAtPath<T>()`。

### 1.4 对“设字段/挂引用”走通用 SerializedObject
不要为 Sprite/Audio/Material 等每种类型写一个 binder。
实现通用命令：`SetSerializedProperty`（支持基本类型、enum、UnityEngine.Object 引用、数组、嵌套字段）。

### 1.5 安全与可控
- 限制可写入的资产路径根目录（例如只允许 `Assets/AutoGen/`）。
- 对所有来自 Job 的绝对路径要归一化并校验，不允许写到 Unity 工程外。
- 记录所有执行日志，必要时可暂停 Runner。

---

## 2. 目录结构（Unity 工程内）

把所有脚本放在 `Assets/Editor/AutoGenJobs/`，建议结构：

Assets/Editor/AutoGenJobs/
  Core/
    AutoGenSettings.cs
    JobRunner.cs
    JobQueue.cs
    JobFileWatcher.cs (可选)
    JobModels.cs
    JobResultModels.cs
    JsonUtil.cs
    PathUtil.cs
    TypeResolver.cs
    AssetRef.cs
    Logging.cs
    Guards.cs
  Commands/
    IJobCommand.cs
    CommandRegistry.cs
    Builtins/
      Cmd_ImportAssets.cs
      Cmd_CreateScriptableObject.cs
      Cmd_SetSerializedProperty.cs
      Cmd_CreateOrEditPrefab.cs
      Cmd_InstantiatePrefabInScene.cs
      Cmd_CreateGameObject.cs
      Cmd_AddComponent.cs
      Cmd_SetTransform.cs
      Cmd_SaveAssets.cs
      Cmd_PingObject.cs (调试)
  UI/
    AutoGenJobsWindow.cs (可选)
    MenuItems.cs
  Tests/
    EditMode/
      JobRunnerTests.cs

---

## 3. 外部与 Unity 的“文件协议”（Job 生命周期）

### 3.1 约定的目录（工程根目录下）
在 Unity 项目根目录创建：

/AutoGenJobs/
  inbox/      // 外部投递 job
  working/    // Unity 拿走执行中的 job（避免重复消费）
  done/       // 执行完成的 job 归档
  results/    // 结果输出（json + log）
  dead/       // 无法处理/协议错误/超限等，进入死信

> 路径可配置，但默认如上。

### 3.2 投递方式（防止半写入）
外部必须：
1) 先写 `*.pending`（临时文件）
2) 写完并 fsync 后，原子 rename 为 `*.job.json`

Unity Runner 只扫描 `*.job.json`。
如果发现 `.pending`，永远不读取。

### 3.3 状态文件（可选但建议）
Runner 执行时在 `working/` 旁边写一个轻量状态：
- `<jobId>.state.json`：RUNNING / WAITING / DONE / FAILED + 当前 command index
用来实现断点恢复或诊断（最小版可不做恢复，但必须写结果）。

---

## 4. Job JSON 协议草案（Schema v1）

### 4.1 顶层结构
```json
{
  "schemaVersion": 1,
  "jobType": "AutoGen",
  "jobId": "2026-01-07T12-34-56Z__abcd1234",
  "createdAtUtc": "2026-01-07T12:34:56Z",
  "runnerMinVersion": 3,
  "requiresTypes": [
    "UnityEngine.SpriteRenderer",
    "MyGame.Runtime.SomeComponent, Assembly-CSharp"
  ],
  "projectWriteRoot": "Assets/AutoGen",
  "dryRun": false,
  "commands": [
    { "cmd": "ImportAssets", "args": { "paths": ["Assets/AutoGen/Images/icon.png"] } },
    { "cmd": "CreateGameObject", "args": { "name": "AutoGen_Object", "parentPath": "" }, "out": { "go": "$go1" } },
    { "cmd": "AddComponent", "args": { "target": { "ref": "$go1" }, "type": "UnityEngine.SpriteRenderer" }, "out": { "component": "$sr1" } },
    { "cmd": "SetSerializedProperty", "args": { "target": { "ref": "$sr1" }, "propertyPath": "m_Sprite", "value": { "assetGuid": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" } } }
  ],
  "meta": {
    "author": "local-ai",
    "note": "create sprite object"
  }
}

### 4.2 变量与引用（out/ref）

* 每条 command 可以通过 `"out": { "name": "$var" }` 导出引用
* 后续通过 `{ "ref": "$var" }` 使用
* Runner 内部维护 `Dictionary<string, UnityEngine.Object>` 的临时引用表（仅本 job 生命周期有效）
* 允许用路径定位现有对象（例如 scene hierarchy path 或 asset path），但优先 ref。

---

## 5. Job 结果协议（Result JSON）

Runner 必须在 results/ 写入：

* `<jobId>.result.json`
* `<jobId>.log.txt`（或 `.log.jsonl`）

Result JSON 示例：

```json
{
  "schemaVersion": 1,
  "jobId": "....",
  "status": "DONE", // DONE | FAILED | WAITING
  "startedAtUtc": "....",
  "finishedAtUtc": "....",
  "runnerVersion": 3,
  "unityVersion": "2022.3.XXf1",
  "message": "ok",
  "commandResults": [
    { "index": 0, "cmd": "ImportAssets", "status": "DONE", "message": "" },
    { "index": 1, "cmd": "CreateGameObject", "status": "DONE", "outputs": { "go": "SceneObject:AutoGen_Object" } }
  ],
  "error": null
}
```

FAILED 时：

```json
"error": { "code": "CMD_EXEC_ERROR", "message": "...", "stack": "..." }
```

WAITING 时必须写明原因：

* `WAITING_COMPILING`
* `WAITING_RUNNER_VERSION`
* `WAITING_TYPES_MISSING`

---

## 6. Runner 核心行为（必须实现）

### 6.1 启动与循环

* 使用 `[InitializeOnLoad]` 静态构造启动。
* 使用 `EditorApplication.update += Tick`。
* Tick 内每次只处理有限 job（例如最多 1 个 job 或最多 30ms），避免卡编辑器。

### 6.2 扫描与锁

* 扫描 `inbox/*.job.json`（按 createdAt 或文件名排序）
* 对某个 job：先 Move 到 `working/`（同卷原子移动优先），确保不会重复消费。
* 如果 Move 失败（被别的进程抢）则跳过。

### 6.3 执行门槛（先检查，后执行）

在消费 job 前必须检查：

1. `EditorApplication.isCompiling` 或 `EditorApplication.isUpdating` -> WAITING（不移动/或移动回 inbox）
2. `runnerVersion < runnerMinVersion` -> WAITING
3. `requiresTypes` 任一解析不到 -> WAITING
4. `projectWriteRoot` 不合法（不在允许根下）-> FAILED（dead/）

### 6.4 幂等与重复 job

* 用 `jobId` 作为唯一键。
* 若 results/ 已存在 `<jobId>.result.json` 且 DONE，则重复投递应直接忽略或返回 DONE（避免重复创建资产）。
* 执行中崩溃重启：可简单地把 working/ 的 job 移回 inbox（或在启动时扫描 working/，按策略恢复）。

### 6.5 Undo/Dirty/Save

* 编辑资产/场景对象应尽量使用 Undo（`Undo.RecordObject` / `Undo.RegisterCreatedObjectUndo`）。
* 修改后 `EditorUtility.SetDirty(obj)`。
* 按需 `AssetDatabase.SaveAssets()`，并可作为命令显式触发（避免每步都 save）。

---

## 7. 命令系统（Command Registry + Builtins）

### 7.1 IJobCommand 接口

* 每个命令实现：

  * `string Name`
  * `CommandExecResult Execute(CommandContext ctx, JObject args)`
* `CommandContext` 提供：

  * settings、logger、varTable、dryRun
  * helper：ResolveTarget / ResolveAsset / ResolveType / SetSerializedProperty

### 7.2 CommandRegistry

* 使用 `TypeCache.GetTypesDerivedFrom<IJobCommand>()` 自动注册（如果 Unity 版本不支持 TypeCache，fallback 反射扫描）。
* 禁止在 runner 主逻辑中写 switch-case 路由。

---

## 8. 内置命令清单（第一阶段必须完成）

以下命令是最小可用集（MVP），必须实现：

### 8.1 ImportAssets

用途：触发 AssetDatabase 刷新/导入
Args:

* paths: string[] (asset path)
* force: bool (optional)
  行为：
* 校验路径在 Assets/ 下
* `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)`（force）或 `AssetDatabase.ImportAsset(path)`

### 8.2 CreateScriptableObject

Args:

* type: string（AssemblyQualifiedName 或完整名）
* assetPath: string（必须在 projectWriteRoot 下）
* overwrite: bool
* init: object（可选：一组字段赋值，最终调用 SetSerializedProperty）
  Outputs:
* asset: $var

### 8.3 CreateGameObject

Args:

* name: string
* parentPath: string（可选，Hierarchy path，如 "Root/Child"；为空则 root）
* position/rotation/scale（可选）
  Outputs:
* go: $var

### 8.4 AddComponent

Args:

* target: { ref: "$go" } 或 scenePath 定位
* type: string
* ifMissing: bool（默认 true）
  Outputs:
* component: $var

### 8.5 SetTransform

Args:

* target: ref
* position/rotation/scale（局部 or 世界可选）
* space: "local"|"world"

### 8.6 SetSerializedProperty（核心通用）

Args:

* target: ref（Component / SO / GameObject）
* propertyPath: string（SerializedProperty path）
* value: union

  * number/string/bool
  * { "enum": "EnumType", "name": "ValueName" }
  * { "assetGuid": "..." } 或 { "assetPath": "Assets/..." }
  * { "ref": "$var" }（引用另一个 object）
  * array/object（递归支持：如果 property 是数组或嵌套结构）
    行为：
* `var so = new SerializedObject(target)`
* `var p = so.FindProperty(propertyPath)` -> null 则失败
* 按 p.propertyType 写入
* `so.ApplyModifiedProperties()`
* `EditorUtility.SetDirty(target)`

### 8.7 CreateOrEditPrefab

Args:

* prefabPath: string（允许新建或编辑）
* rootName: string（新建时用）
* edits: command[]（嵌套命令，仅作用于 prefab contents root）
  行为：
* 如果 prefab 不存在：创建临时 GO -> SaveAsPrefabAsset
* `PrefabUtility.LoadPrefabContents(prefabPath)` 得到 root
* 在 root 上执行嵌套 edits（允许调用 AddComponent/SetSerializedProperty/EnsureChild 等）
* `PrefabUtility.SaveAsPrefabAsset(root, prefabPath)`
* `PrefabUtility.UnloadPrefabContents(root)`

### 8.8 InstantiatePrefabInScene

Args:

* prefabGuid / prefabPath
* parentPath（可选）
* nameOverride（可选）
  Outputs:
* instance: $var

### 8.9 SaveAssets

Args:

* refresh: bool
  行为：
* `AssetDatabase.SaveAssets()`
* optionally `AssetDatabase.Refresh()`

---

## 9. 第二阶段增强命令（建议实现）

* EnsureHierarchyPath（确保某个层级路径存在）
* RemoveComponent / DestroyObject
* SetTagLayer / SetStaticFlags
* ApplyPrefabOverrides / RevertPrefabOverrides
* OpenSceneAdditive / CloseScene（谨慎，默认不切换 active scene）
* PingObject（EditorGUIUtility.PingObject）
* SelectObject（Selection.activeObject）

---

## 10. 场景策略（必须遵守）

默认策略：**不切场景**，只对当前 Active Scene 操作。

* CreateGameObject / InstantiatePrefabInScene 都应落到 `SceneManager.GetActiveScene()`
* 除非 Job 明确指定 `scenePath`，才允许加载 scene（并且优先 additive，避免抢走用户正在编辑的场景上下文）。

---

## 11. 错误处理与死信策略

### 11.1 可恢复（WAITING）

* Unity compiling
* runnerVersion 不够
* requiresTypes 缺失（等待编译完成）

### 11.2 不可恢复（FAILED -> dead/）

* JSON 解析失败、schemaVersion 不支持
* projectWriteRoot 不合法、越权写入
* 命令未知（cmd 不存在）
* SetSerializedProperty 找不到 propertyPath（除非命令显式允许 ignoreMissing）

### 11.3 失败要输出足够诊断信息

* 哪条命令失败（index）
* args 摘要（避免输出超大字段）
* stack trace
* 当前 Unity 状态：isCompiling/isUpdating

---

## 12. Settings（AutoGenSettings.cs）

用 ScriptableObject 或 EditorPrefs 记录配置：

* jobRootAbsolute（默认 projectRoot/AutoGenJobs）
* allowedWriteRoots（默认 ["Assets/AutoGen"]）
* tickIntervalMs（默认 200ms）
* maxJobsPerTick（默认 1）
* maxMsPerTick（默认 20~30ms）
* enableRunner（可开关）
* verboseLogging

提供菜单：

* Tools/AutoGen Jobs/Open Jobs Folder
* Tools/AutoGen Jobs/Toggle Runner
* Tools/AutoGen Jobs/Show Window

---

## 13. 兼容性注意事项（Unity API）

* Tick 中不要执行耗时 IO（大文件拷贝、网络下载），这些应该由外部完成，Unity 只负责导入与绑定。
* 使用 `AssetDatabase` / `PrefabUtility` / `SerializedObject` 的调用必须在主线程（Editor 主线程）执行。
* 使用 `TypeCache` 需要较新 Unity；若不可用，用反射扫描当前 domain assemblies。

---

## 14. 测试计划（EditMode Tests）

必须写最少 3 个 EditMode 测试：

1. CreateScriptableObject + SetSerializedProperty（基本类型）
2. CreateOrEditPrefab：AddComponent + SetSerializedProperty（对象引用用 GUID）
3. InstantiatePrefabInScene + SetTransform（确认落到 active scene）

测试要做到：

* 执行前清理 `Assets/AutoGen/Tests/`
* 执行后可重复跑（幂等/覆盖策略明确）

---

## 15. 实现路线（一步步交付）

### Milestone A（1 天内可完成）

* Settings + Runner Tick + inbox -> working -> results 的全链路
* JSON 解析 + WAITING 门槛
* ImportAssets / SaveAssets
* CreateGameObject / AddComponent / SetTransform

### Milestone B（可用）

* SetSerializedProperty（核心）
* CreateScriptableObject
* InstantiatePrefabInScene

### Milestone C（完善）

* CreateOrEditPrefab（LoadPrefabContents）
* CommandRegistry 自动发现
* UI Window + 日志检索
* EditMode Tests 完整跑通

---

## 16. 示例 Job（可直接用于外部生成）

### 16.1 创建一个 SO 并赋值

```json
{
  "schemaVersion": 1,
  "jobType": "AutoGen",
  "jobId": "demo_create_so_001",
  "createdAtUtc": "2026-01-07T12:00:00Z",
  "runnerMinVersion": 3,
  "requiresTypes": ["MyGame.Configs.ItemConfig, Assembly-CSharp"],
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateScriptableObject",
      "args": {
        "type": "MyGame.Configs.ItemConfig, Assembly-CSharp",
        "assetPath": "Assets/AutoGen/Configs/Item_Sword.asset",
        "overwrite": true
      },
      "out": { "asset": "$item" }
    },
    {
      "cmd": "SetSerializedProperty",
      "args": { "target": { "ref": "$item" }, "propertyPath": "displayName", "value": "Sword" }
    },
    {
      "cmd": "SaveAssets",
      "args": { "refresh": true }
    }
  ]
}
```

### 16.2 导入 Sprite 并创建场景对象绑定 SpriteRenderer.sprite

```json
{
  "schemaVersion": 1,
  "jobType": "AutoGen",
  "jobId": "demo_sprite_001",
  "createdAtUtc": "2026-01-07T12:00:00Z",
  "runnerMinVersion": 3,
  "requiresTypes": ["UnityEngine.SpriteRenderer"],
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    { "cmd": "ImportAssets", "args": { "paths": ["Assets/AutoGen/Images/icon.png"], "force": true } },
    { "cmd": "CreateGameObject", "args": { "name": "AutoGen_Sprite" }, "out": { "go": "$go" } },
    { "cmd": "AddComponent", "args": { "target": { "ref": "$go" }, "type": "UnityEngine.SpriteRenderer" }, "out": { "component": "$sr" } },
    {
      "cmd": "SetSerializedProperty",
      "args": {
        "target": { "ref": "$sr" },
        "propertyPath": "m_Sprite",
        "value": { "assetPath": "Assets/AutoGen/Images/icon.png" }
      }
    }
  ]
}
```

### 16.3 编辑/新建 Prefab，挂组件并设置字段，然后实例化到场景

```json
{
  "schemaVersion": 1,
  "jobType": "AutoGen",
  "jobId": "demo_prefab_001",
  "createdAtUtc": "2026-01-07T12:00:00Z",
  "runnerMinVersion": 3,
  "requiresTypes": ["MyGame.Runtime.Health, Assembly-CSharp"],
  "projectWriteRoot": "Assets/AutoGen",
  "commands": [
    {
      "cmd": "CreateOrEditPrefab",
      "args": {
        "prefabPath": "Assets/AutoGen/Prefabs/Enemy.prefab",
        "rootName": "Enemy",
        "edits": [
          { "cmd": "AddComponent", "args": { "target": { "ref": "$prefabRoot" }, "type": "MyGame.Runtime.Health, Assembly-CSharp" }, "out": { "component": "$hp" } },
          { "cmd": "SetSerializedProperty", "args": { "target": { "ref": "$hp" }, "propertyPath": "maxHp", "value": 50 } }
        ]
      }
    },
    {
      "cmd": "InstantiatePrefabInScene",
      "args": { "prefabPath": "Assets/AutoGen/Prefabs/Enemy.prefab" },
      "out": { "instance": "$enemy" }
    },
    { "cmd": "SetTransform", "args": { "target": { "ref": "$enemy" }, "position": [0, 0, 0], "space": "world" } }
  ]
}
```

> 注意：CreateOrEditPrefab 的嵌套 edits 里需要一个约定：prefab root 自动放入 `$prefabRoot` 变量（由命令实现注入），这样 edits 里无需先 out 它。

---

## 17. 交付物清单（你最终要提交的代码）

* JobRunner：可开关、可诊断、稳定 tick
* JobModels：Job / Command / TargetRef / ValueUnion 的序列化结构
* Command 系统：Registry + 内置命令（至少 MVP）
* SerializedProperty 写值工具：覆盖最常见类型 + Object 引用（GUID/path/ref）
* Prefab 编辑管线：LoadPrefabContents / Save / Unload
* 结果输出：result.json + log
* Settings + 菜单 + 可选 Window
* 3 个 EditMode Tests

---

## 18. 额外要求（非常重要）

* 任何命令执行失败：必须立刻停止后续命令并输出 FAILED result。
* 所有日志必须带 jobId + command index。
* Runner 在编译/更新期间不得消费新 job。
* 默认不改变用户当前场景，不抢焦点，不弹窗（除非 verbose/debug 模式）。
* 所有创建资产必须在 allowedWriteRoots 下，违规直接 dead-letter。

完成。

