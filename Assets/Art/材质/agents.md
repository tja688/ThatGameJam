
> 通用前提（给AI的工程约束）

* Unity 6.3 LTS + URP（2D Renderer，开启2D Light）
* 用 **Full Screen Pass Renderer Feature** 注入（建议先用 After Rendering / After Post-Processing 这类能看到最终画面的注入点，具体让AI按你项目现状选）
* Shader 必须采样 Full Screen Pass 提供的屏幕纹理（常见命名 `_BlitTexture` + sampler），输出到全屏
* 所有效果都要做成 **单材质可跑**，可选功能用 keyword/float 开关

---

## 需求单 01：灯火柔光与溢光晕染（Soft Bloom + Halation）

**目标效果**：灯、亮边、亮点柔和泛光；高亮有一点暖色“晕边”，暗部不脏。
**输入**：屏幕颜色（_BlitTexture）。
**输出**：处理后的颜色。
**核心做法**（允许简化版）：

* 从屏幕颜色提取高亮（阈值 + soft knee）
* 做小半径模糊（建议 5~13 taps 的一维/近似高斯；禁止多RT复杂管线，先单pass近似）
* 将模糊结果以可控强度叠回原图（Add / Screen）
* Halation：对高亮做轻微暖色偏移（例如偏黄/红的权重），让亮边“发暖”
  **参数**：
* `Threshold`（0~2）
* `Knee`（0~1）
* `Radius`（0~3，控制采样距离）
* `Intensity`（0~2）
* `HalationTint`（Color）
* `HalationStrength`（0~1）
  **默认建议**：Threshold=1.0, Knee=0.5, Radius=1.2, Intensity=0.6, HalationStrength=0.25
  **注意**：要避免整屏发灰/发糊；强度宁低，突出“灯”。

---

## 需求单 02：冷暗+暖灯的电影调色（Split Toning + Filmic Contrast）

**目标效果**：阴影偏冷（青蓝/墨绿），高光偏暖（橙黄），对比更有层次但不死黑。
**输入**：屏幕颜色。
**输出**：调色后颜色。
**核心做法**：

* 计算亮度 `luma`
* 用 `luma` 做权重：阴影区混入 `ShadowColor`，高光区混入 `HighlightColor`
* 叠加 Filmic 对比：S曲线或简单 tone mapping（Reinhard/ACES简化都行）
* 可选：轻微抬黑（Lift）防止暗部糊成一片
  **参数**：
* `ShadowColor`（Color）
* `HighlightColor`（Color）
* `Balance`（0~1，阴影/高光分界）
* `Contrast`（0.5~1.5）
* `Lift`（-0.1~0.2）
* `Saturation`（0~2）
  **默认建议**：Shadow=(0.10,0.22,0.28)，Highlight=(1.0,0.72,0.35)，Balance=0.45，Contrast=1.1，Lift=0.05，Sat=1.05

---

## 需求单 03：2D友好的空气雾（Screen-space Haze）

**目标效果**：远处更雾、更冷，近处更清晰；立刻增加纵深。
**一键版本（必须先做这个）**：只按屏幕Y（或自定义渐变方向）上雾，不依赖深度。✅
**输入**：屏幕颜色。
**输出**：加雾后颜色。
**核心做法**：

* 用屏幕UV的 `uv.y` 计算雾因子（可反转）
* 雾因子做曲线（pow/smoothstep）增强可控性
* 将颜色向 `FogColor` 混合
* 可选：让高亮区域雾更淡（用亮度反向削弱雾），模拟“灯把雾烘开”
  **参数**：
* `FogColor`（Color）
* `FogStartY` / `FogEndY`（0~1）
* `FogDensity`（0~2）
* `FogCurve`（0.5~4）
* `HighlightClear`（0~1）
  **进阶可选（不是必须）**：如果你们愿意开 Depth Texture，再做“按深度雾”。⚠️

---

## 需求单 04：油画块面（Kuwahara / Painterly 简化）

**目标效果**：画面被“揉成块面”，像绘本/油画，压掉原型感。
**输入**：屏幕颜色。
**输出**：块面化后的颜色。
**核心做法（允许近似）**：

* 实现 Kuwahara 或其快速变体（建议快速版，控制采样数）
* 至少提供 `KernelSize`（3/5/7/9）可调
* 可选：边缘保护（在高梯度处减弱滤波）
  **参数**：
* `KernelSize`（3~12）
* `EdgePreserve`（0~1）
* `Blend`（0~1，和原图混合）
  **默认建议**：Kernel=6，EdgePreserve=0.6，Blend=0.85
  **注意**：性能风险最大；先做低采样可用版。

---

## 需求单 05：水彩纸纹 + 轻渗色（Watercolor Paper + Bleed）

**目标效果**：大色块更“有材质”，边缘略晕开，整体温柔。
**输入**：屏幕颜色；可选纸纹贴图（PaperTex）。
**输出**：水彩化颜色。
**核心做法**：

* 纸纹：用 PaperTex 或程序噪声，做 very subtle 的乘/叠加（不要抢戏）
* 渗色：对颜色做一个极轻的局部扩散（可以用小半径模糊再按边缘权重混合）
* 可选：颜色轻微去饱和 + 提亮中间调
  **参数**：
* `PaperStrength`（0~0.3）
* `BleedRadius`（0~2）
* `BleedStrength`（0~1）
* `Blend`（0~1）
* `PaperTexTiling`（1~8）
  **默认建议**：Paper=0.12，BleedRadius=0.8，Bleed=0.25，Blend=0.9

---

## 需求单 06：渐变映射（Gradient Map）

**目标效果**：一键统一色调层次（暗→冷，高光→暖），非常适合你们现在的蓝绿空洞问题。
**输入**：屏幕颜色；可选 1D 渐变纹理（GradientTex）。
**输出**：映射后的颜色。
**核心做法**：

* 计算亮度 luma
* 用 luma 去采样 GradientTex（或用 3~5 个颜色节点做插值）
* 与原色按 `Strength` 混合
  **参数**：
* `Strength`（0~1）
* `GradientTex`（可选）
* `PreserveSaturation`（0~1）
  **默认建议**：Strength=0.75，PreserveSat=0.35

---

## 需求单 07：限色 + 抖动（Posterize + Dither）

**目标效果**：复古梦境/强风格，立刻提升辨识度。
**输入**：屏幕颜色；可选 Bayer 8x8/4x4 抖动纹理（也可硬编码矩阵）。
**输出**：限色抖动后的颜色。
**核心做法**：

* 先做色阶量化（每通道或按亮度）
* 用抖动阈值（Bayer）对量化误差做空间分布
* 可选：只对暗部更强（保留灯区干净）
  **参数**：
* `Levels`（2~16）
* `DitherStrength`（0~1）
* `DitherScale`（1~4）
* `HighlightProtect`（0~1）
  **默认建议**：Levels=8，Dither=0.35，Scale=2，HighlightProtect=0.6

---

## 需求单 08：插画描边（Edge Ink，2D可用版）

**目标效果**：物体边缘出现细描线，增强可读性。
**必须用2D友好方案**：基于颜色/亮度差做边缘检测（Sobel/Roberts）。✅
**输入**：屏幕颜色。
**输出**：叠加线稿后的颜色。
**核心做法**：

* 对周围像素做 Sobel 得到边缘强度
* 用阈值控制是否出线
* 线颜色不要纯黑，建议深蓝/深青
* 与原图以 `InkStrength` 混合
  **参数**：
* `EdgeWidth`（0.5~2.0，采样步长）
* `Threshold`（0~1）
* `InkColor`（Color）
* `InkStrength`（0~1）
* `OnlyDarken`（0/1，可选：只压暗不发亮）
  **默认建议**：Width=1.0，Threshold=0.18，Ink=(0.02,0.08,0.10)，Strength=0.55

---

## 需求单 09：暗角 + 轻锐化（Vignette + Subtle Sharpen）

**目标效果**：收束视线、提升“镜头语言”，顺便让主体更清晰。
**输入**：屏幕颜色。
**输出**：处理后颜色。
**核心做法**：

* 暗角：按到屏幕中心距离做 smoothstep 混合
* 锐化：简单 unsharp mask（原图 - 模糊图）* amount
  **参数**：
* `VignetteStrength`（0~0.8）
* `VignetteRoundness`（0~1）
* `SharpenAmount`（0~1）
* `SharpenRadius`（0~2）
  **默认建议**：Vig=0.25，Round=0.6，Sharp=0.18，Radius=0.8

---

## 需求单 10：梦境镜头（Chromatic Aberration + Gentle Distortion）

**目标效果**：轻微色散 + 极轻扭曲/呼吸感，增强“非现实”。
**输入**：屏幕颜色；可选时间 `_Time`（默认可用）。
**输出**：处理后颜色。
**核心做法**：

* 色散：R/G/B 采样略微偏移（径向向外）
* 扭曲：用一个低频噪声或正弦对 UV 做轻微偏移（强度极低）
  **参数**：
* `CA_Strength`（0~1，实际要很小）
* `DistortStrength`（0~1）
* `DistortSpeed`（0~2）
* `DistortScale`（0.5~4）
* `EdgeOnly`（0~1：越靠近边缘越强）
  **默认建议**：CA=0.12，Distort=0.08，Speed=0.3，Scale=1.2，EdgeOnly=0.7

---

## 需求单 11：胶片颗粒 + 尘埃（Film Grain + Dust）

**目标效果**：暗部有质感，不再“太干净像原型”。
**输入**：屏幕颜色；可选噪声贴图（或程序噪声）；可选时间。
**输出**：叠加颗粒后的颜色。
**核心做法**：

* 生成噪声（纹理或 hash）
* 让颗粒强度随亮度变化（暗部更明显）
* 可选：偶发大一点的尘埃点（非常少）
  **参数**：
* `GrainStrength`（0~0.6）
* `GrainSize`（0.5~4）
* `LumaResponse`（0~1，暗部增强）
* `DustStrength`（0~0.3）
  **默认建议**：Grain=0.18，Size=1.6，LumaResp=0.7，Dust=0.06

---

## 需求单 12：灯区光域增强（Fake Light Shafts / Scatter）

**目标效果**：灯周围有一点“散射/条纹”，强化灯是安全锚点。
**输入**：屏幕颜色。
**输出**：增强后颜色。
**核心做法（轻量版）**：

* 从高亮提取源（类似 Bloom 的阈值）
* 沿着从中心点或屏幕某方向做少量步进采样累加（8~16步即可）
* 强度要非常轻，只在灯这种高亮处出现
  **参数**：
* `Threshold`（0~2）
* `Steps`（4~24）
* `ScatterStrength`（0~1）
* `Decay`（0~1）
* `Tint`（Color）
  **默认建议**：Threshold=1.1，Steps=12，Strength=0.18，Decay=0.85，Tint偏暖

---

# “交付物”验收

**每个效果交付物必须包含：**

1. 一个 Shader（URP 兼容，全屏采样 `_BlitTexture`）
2. 一个 Material（默认参数已经“好看可用”）
3. 参数都有 Range，Inspector 里好调
4. 写一段 5 行以内的用法说明：把材质塞进 Full Screen Pass 即可

