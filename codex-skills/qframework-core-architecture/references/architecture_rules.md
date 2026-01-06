# Core Architecture Rules Summary

Sources:
- `Assets/QFramework/QFramework_Docs_用户手册.md` (Architecture chapters, "架构规范 与 推荐用法")
- `Assets/QFramework/Framework/Scripts/QFramework.cs`
- `Assets/QFramework/QFramework_Docs_API.md`

Core concepts:
- `Architecture<T>` manages module registration and access.
- Layers: `IController`, `ISystem`, `IModel`, `IUtility`.
- CQRS: Commands for writes, Queries for reads.
- Events and `BindableProperty` for state-change notifications.
- `TypeEventSystem` and unregistration helpers for safe event lifecycles.

Rules:
- Controllers may depend on Systems/Models/Utilities; lower layers must not reference Controllers.
- Models stay pure; persistence and external I/O live in Utilities.
- Commands and Queries must be stateless.

Verification keywords:
- `Architecture<`
- `IController`
- `ISystem`
- `IModel`
- `IUtility`
- `AbstractCommand`
- `AbstractQuery`
- `TypeEventSystem`
- `BindableProperty`
- `UnRegisterWhenGameObjectDestroyed`
