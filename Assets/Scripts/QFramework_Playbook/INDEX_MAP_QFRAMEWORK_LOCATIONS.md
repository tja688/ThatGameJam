# INDEX_MAP_QFRAMEWORK_LOCATIONS.md — Evidence & Navigation Map

### Attention, if you have not read AGENTS.md yet, please read AGENTS.md before reading this document

## 0. 项目内权威 API Reference（必须优先查证）

本项目包含一套与“当前仓库 QFramework 版本”匹配的 API 文档。
当 AI 需要确认 **API 是否存在 / 签名是否正确 / 是否已废弃 / 推荐用法** 时：

**必须优先查：本项目内的 API Reference**  
其次才查：QFramework 源码（只读）  
最后才参考：互联网资料（仅作补充，且必须说明可能不一致）

### API 文档位置（项目内权威）

- `Assets/QFramework/QFramework_Docs_API.md` 

### 查证流程（AI 必须遵循）

1) 在 API 文档里检索：类名 / 方法名 / 字段名（含大小写）
2) 若文档缺失或无法确认：
   - 转查 QFramework 源码（只读）：
     - `Assets/QFramework/Framework/Scripts/`（或实际源码目录）
3) 若仍无法确认：
   - 标记 `UNVERIFIED`
   - 输出 `NEXT_SEARCH`：建议用户提供该 API 文档入口或截图/路径

---

## 1. Local Documents Reference

| Topic | Primary Source (Local Path) |
| :--- | :--- |
| **QFramework Architecture** | `Assets/QFramework/QFramework_Docs_用户手册.md` |
| **4-Layer Rules** | `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/2. 架构篇：QFramework.cs/10. 架构规范 与 推荐用法.md` |
| **UIKit Usage** | `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/4. 解决方案篇/02. UIKit：界面管理&快速开发解决方案/01. 简介与快速入门.md` |
| **ResKit Usage** | `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/4. 解决方案篇/01. ResKit：资源管理&开发解决方案/01. 简介与快速入门.md` |
| **ActionKit Usage** | `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/3. 工具篇：QFramework.Toolkits/04. ActionKit 时序动作执行系统/01. 简介.md` |

---

## 2. Source Code Entry Points (ReadOnly)

| Identifier | Location in `Assets/QFramework/Framework/Scripts/QFramework.cs` |
| :--- | :--- |
| `Architecture<T>` | Core container for module registration. |
| `IController` | Entry interface for MonoBehaviours. |
| `IModel` / `ISystem` | Business logic layer markers. |
| `AbstractCommand` | Base class for state mutation logic. |
| `AbstractQuery` | Base class for side-effect-free data retrieval. |
| `TypeEventSystem` | Global and local event bus implementation. |
| `UnRegisterExtension` | Definitions for `UnRegisterWhenGameObjectDestroyed`, etc. |

---

## 3. Evidence Mapping

- **Vertical Slice Strategy**: Implementation preference for this project.
- **RootApp Singleton**: Enforced by Project Rules to avoid container fragmentation.
- **Singleton Restriction**: Architecture is the source of truth; `SingletonKit` is infrastructure only.
- **UIKit Bindings**: Standardized by QFramework CodeGenKit to minimize `transform.Find`.
