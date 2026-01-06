# ActionKit Rules Summary

Sources:
- `Assets/QFramework/QFramework_Docs_用户手册.md` (ActionKit chapters)
- `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/3. 工具篇：QFramework.Toolkits/04. ActionKit 时序动作执行系统/*.md`

Key concepts:
- Build sequences with Delay, Sequence, Parallel, Repeat, DelayFrame, Condition.
- Start actions with Component/GameObject or global start helpers.
- Use lifecycle-aware starts to avoid leaks on destroy.
- Optional integrations: DOTween, UniRx, and IgnoreTimeScale.

Verification keywords:
- "ActionKit.Sequence"
- "ActionKit.Delay"
- "ActionKit.Parallel"
- "ActionKit.Repeat"
- "DelayFrame"
- "StartGlobal"
- "StartCurrentScene"
