# AGENTS.md — Spine 角色动画自动分析与 Unity 适配驱动

## 0. 任务目标（今晚交付版）

我们有一个已经导入 Unity 的 Spine 2D 主角资源（动画复杂、层级多、存在可叠加动画、可用 Alpha 权重做过渡/幅度控制）。美术联系不上，但策划要求今晚必须能玩到“完整基础动作 + 常见组合”的角色动画表现。

你的目标是：

1. **自动分析 Spine 文件/Unity Spine 资源**，列出当前包含的动画（animations）、皮肤（skins）、事件（events）（如有），并从命名/结构特征推测其用途。
2. **将动画按“平台跳跃角色”需求归类**：Idle/Run/JumpUp/Fall/Land/Climb/… 以及可叠加层（呼吸/眨眼/上半身动作/持物/受击等）。
3. **在 Unity 用 Spine 插件的“原生 AnimationState 路线”实现一个驱动脚本**：监听角色“动作意图”（输入/速度/是否落地/是否攀爬等）并切换/混合动画。
4. **交付可用，不追求极致自然**：混合和权重先用保守默认值跑通；后续再细调。

> 重要：这是纯视觉动画驱动，不做 IK、不做真实骨骼物理，只要按键/状态切换时 Spine 动画能跟上即可。

---

## 1. 你可以使用/必须遵守的约束

* **优先使用 Spine-Unity Runtime 的 API（SkeletonAnimation / SkeletonMecanim / AnimationState / TrackEntry 等）**，不要引入额外第三方动画系统。
* 不要大规模重构角色控制器；适配脚本应 **“读控制器输出/意图”**，而不是重写移动逻辑。
* 改动尽量局部：新增脚本/少量挂载/少量引用即可。
* 交付优先级：**能跑 > 完整覆盖动作 > 基础混合 > 美术级细腻**。
* 如果发现项目里 Spine 用的是 Unity Animator（Mecanim）路线，也要能识别并改为 **AnimationState 驱动** 或提供最小修改方案继续走原路线（但默认优先 AnimationState）。

---

## 2. 首先做一次“资源与运行时路径确认”

你需要在项目中确认 Spine 的使用组件是哪一种（至少确认一种）：

* `SkeletonAnimation`（最常见，2D 场景对象）
* `SkeletonGraphic`（UI）
* `SkeletonMecanim`（走 Unity Animator）
* 以及是否存在 `SpineAnimationState` / `AnimationState` 相关脚本挂载

### 你必须输出一段简短结论：

* 角色对象上挂的 Spine 组件类型是什么
* 动画是通过 Unity Animator 控制还是通过 Spine `AnimationState` 控制
* Spine 资源主要是 `.json/.skel.bytes + .atlas` 还是已经打成 `SkeletonDataAsset`

---

## 3. 自动分析 Spine 动画：两条路线（尽量都做）

### 路线 A：命名与元信息归类（最快、今晚可交付）

1. 从 `SkeletonDataAsset` 里拿到 `SkeletonData.Animations` 列表。

2. 对每个动画输出：

   * 动画名（原名）
   * 时长（duration）
   * 是否看起来像循环（可用命名/时长/采样判断）
   * 可能的类别（Idle/Run/Jump/Fall/Land/Climb/Attack/Hit/Interact/Overlay…）
   * 置信度（高/中/低）
   * 建议使用方式：Base Track（0）还是 Overlay Track（1/2…）

3. 命名规则推断（你需要内置一套关键词表，大小写不敏感，支持常见缩写）：

* Idle：`idle, stand, wait, breathe`
* Run/Walk：`run, walk, move, locomotion`
* Jump：`jump, takeoff, up`
* Fall：`fall, drop, down, air`
* Land：`land, landing`
* Climb：`climb, ladder, rope`
* Crouch：`crouch, squat`
* Hit/Hurt/Die：`hit, hurt, damage, death, die, knock`
* Attack：`attack, atk, slash, shoot`
* Interact：`use, interact, pickup, push, pull`
* Overlay/UpperBody：`upper, torso, aim, hold, carry, additive, overlay, layer`
* Facial：`blink, eye, face, mouth, expression`

> 注意：美术命名可能很随意。命名只作为第一优先级推断，必须允许“未知/待确认”。

---

### 路线 B：基于“骨骼/关键帧运动特征”的采样归类（更准，尽量做）

如果你能做到，这会让分类非常稳。

思路：用 Editor 脚本对每个动画做采样（例如每 0.1s 或 10fps），把动画应用到 Skeleton 上，统计特征：

* Root 或 Hip/Body 主骨骼的位移幅度（X/Y）
* 平均速度/最大速度
* 是否周期性（位移/角度是否呈现循环）
* 上半身骨骼变化 vs 下半身骨骼变化占比（用于判断 overlay 候选）
* 动画事件（events）是否存在（如 footstep）

据此推断：

* X 位移幅度持续大且周期性强 → Run/Walk
* Y 位移存在明显上升/下降峰 → Jump/Fall/Land
* 全身变化小、主要是呼吸轻微摆动 → Idle/Overlay
* 变化集中在上半身（手/臂/武器）→ Overlay（Track 1）

你需要交付一个 **Editor 工具**（菜单项/按钮皆可）：

* 一键扫描该角色 SkeletonDataAsset
* 输出分析报告到控制台 + 写入 `SpineAnimationReport.md`（或 json）到项目某个目录
* 报告里要有“推荐映射表草案”

> 如果采样实现成本过高，至少做路线 A；路线 B 做到多少写多少，并标注哪些推断是“采样证据支持”。

---

## 4. 输出：动作分类与“组合策略”（今晚版）

你需要最终输出一个“平台跳跃动作表”，至少包含：

### 4.1 Base 动作（Track 0）

* Idle（站立）
* Run/Walk（左右移动）
* JumpUp（起跳/上升段）
* Fall（下落段）
* Land（落地瞬间，可选）
* Climb（攀爬，可选：上/下/静止）

### 4.2 Overlay 动作（Track 1/2…，如果资源支持就用）

* Breath/IdleAdd（呼吸、待机小动作）
* Blink/Face（眨眼、表情）
* UpperBody（例如拿东西/瞄准/轻攻击）
* HitReact（受击短暂覆盖）

**组合策略要求**：

* Track 0：只放“全身基础姿态/位移逻辑”的动画（Idle/Run/Jump/Fall/Climb）
* Track 1：放“可叠加”的动画（上半身/表情/呼吸等）
* 使用 `TrackEntry.Alpha` 或 `SetEmptyAnimation` 做淡入淡出
* 如果某 overlay 和 base 冲突严重（比如 overlay 也在控制腿部），必须降级：要么不叠加，要么只在 Idle 上叠

---

## 5. 实现：Unity 里的 Spine 动画驱动脚本

你需要写一个脚本（建议命名）：

* `SpineCharacterAnimDriver.cs`

### 5.1 脚本职责

* 获取 Spine 组件（优先 `SkeletonAnimation`）
* 读取角色当前“动作意图/状态”（来自现有控制器、Rigidbody2D、或输入）
* 根据规则决定 Base 动作（Track 0）和 Overlay 动作（Track 1）
* 做最基础的混合：切换时给 mixDuration、overlay 淡入淡出
* 可选：派发脚步事件、落地事件（如果 Spine 有 events）

### 5.2 动作意图来源（优先级从高到低）

你需要在项目里查现有角色控制器（例如 `PlayerController` / `CharacterMotor2D` 之类），优先直接读它的状态（isGrounded/isClimbing/velocity/…）。如果没有清晰接口，则用保守方案：

* `Rigidbody2D.velocity.x/y`
* 一个 GroundCheck（项目现有的落地判定）
* 输入轴（Horizontal/Jump）仅作为辅助（不要重写输入系统）

最终你要在脚本注释里写清楚：你是从哪个脚本/字段拿到状态的。

### 5.3 最小可用的状态机（必须实现）

使用简单规则即可（不要过度复杂）：

* `isClimbing == true`

  * |velocity.y| > 阈值：ClimbMove（循环）
  * 否则：ClimbIdle（如果有）或 ClimbMove 设 timeScale 很低

* `isGrounded == false`

  * velocity.y > +阈值：JumpUp（不循环或短循环）
  * velocity.y < -阈值：Fall（循环或持续）

* `isGrounded == true`

  * |velocity.x| > 阈值：Run/Walk（循环）
  * 否则：Idle（循环）
  * 如果检测到“刚落地”并且有 Land：播放 Land（非循环）然后回 Idle/Run

### 5.4 需要支持的“快速混合”

* Base Track 0：切动画时设置 `mixDuration`（例如 0.05~0.15）
* Overlay Track 1：淡入淡出（Alpha 从 0 → 1，或 `SetEmptyAnimation` 清空）
* 提供可调参数（Inspector）：

  * speedThreshold / jumpThreshold / fallThreshold
  * baseMixDuration
  * overlayFadeIn / overlayFadeOut
  * 各动画名映射（字符串下拉或手填）

### 5.5 动画名映射：必须做成可配置

因为命名不一定准，你必须让策划/程序能改：

* 做一个 `[Serializable]` 映射结构：

  * IdleName / RunName / JumpUpName / FallName / LandName / ClimbName …
  * Overlay：BreathName / BlinkName / UpperBodyName / HitName …
* 支持自动填充（来自你的分析结果），但允许手动覆盖。

---

## 6. 交付物清单（你必须输出这些）

1. `SpineAnimationReport.md`（或 `.json`）：

   * 动画列表、时长、推测分类、置信度、建议 Track
   * 推荐的映射表草案（Base + Overlay）
   * 如果做了采样：写清采样证据（例如“rootX 变化大/周期性强 → run”）

2. `SpineCharacterAnimDriver.cs`：

   * 可直接挂在角色对象上运行
   * Inspector 可配置动画名与阈值
   * 不依赖美术额外操作即可看到效果

3. （可选但强烈建议）`SpineAnimationScanner.cs`（Editor 工具）：

   * 菜单项：`Tools/Spine/Scan Character Animations`
   * 一键输出报告并尽量自动填充映射

4. 简短使用说明 `README_AnimDriver.md`：

   * 挂载到哪个对象
   * 需要引用哪些组件字段（grounded/velocity/climb）
   * 常见问题：动画名不匹配怎么改、混合太硬怎么调

---

## 7. 质量门槛（今晚验收标准）

* 按 **←/→**：能从 Idle 切到 Run/Walk，松开回 Idle
* 按 **Jump**：离地后能进入 JumpUp/Fall（至少能在空中有一个合理动画）
* 落地：能回到 Idle/Run（有 Land 更好，没有也接受）
* 如果有攀爬系统：进入攀爬时切换到 Climb，离开攀爬回地面逻辑
* 不要求所有叠加动画都用上，但如果资源明显提供了 blink/breath 等，尽量给一个 Track1 示例

---

## 8. 开发提示（你可以用的 Spine Runtime 关键点）

你可以参考这些 Spine-Unity 常用调用方式（具体以项目 Spine 版本 API 为准）：

* 播放基础动画：

  * `AnimationState.SetAnimation(trackIndex, animationName, loop)`
* 排队动画：

  * `AnimationState.AddAnimation(trackIndex, animationName, loop, delay)`
* 混合时长：

  * `var entry = SetAnimation(...); entry.MixDuration = baseMixDuration;`
* 清空 overlay：

  * `AnimationState.SetEmptyAnimation(trackIndex, fadeOutDuration)`
* Overlay 权重：

  * `entry.Alpha = overlayAlpha;`（若版本支持）
* 统一混合表：

  * `AnimationState.Data.SetMix(fromName, toName, duration)`

如果发现角色实际上走 Mecanim（Unity Animator），你需要：

* 输出“继续走 Mecanim 的最小方案”与“切回 AnimationState 的方案”，默认选择成本最低且可按期交付的一条。

---

## 9. 失败兜底策略（必须准备）

如果动画命名非常混乱、采样也无法稳定推断：

* 至少实现一个“动画浏览器/快速切换测试脚本”：

  * 在运行时用按键 `[` `]` 切换动画名
  * 或在 Inspector 下拉选择播放
    这样策划也能快速指出：哪个是跑、哪个是跳、哪个是落地，你再把映射填上。

---

## 10. 你开始执行时的工作顺序（强制）

1. 确认 Spine 组件路线与资源入口（SkeletonDataAsset）
2. 跑路线 A：先生成报告（动画列表 + 命名推断）
3. 视时间决定做路线 B：采样增强推断
4. 输出映射表草案
5. 实现 `SpineCharacterAnimDriver` 并接入现有控制器状态
6. 现场验证四个验收点（Idle/Run/Jump/Fall/落地/攀爬）
7. 写使用说明与可调参数建议

---

## 11. 你需要在最终回复里给我的内容格式

你完成后，给我以下内容（按顺序）：

1. “项目 Spine 路线确认”结论（组件类型/驱动方式/资源入口）
2. `SpineAnimationReport.md` 内容摘要（或文件路径）
3. 推荐映射表（Base + Overlay）
4. 你新增/修改的脚本文件列表（带路径）
5. 如何挂载与如何调参（最短步骤）
6. 仍不确定的点（例如某动画用途不明）与建议的手动确认方式

