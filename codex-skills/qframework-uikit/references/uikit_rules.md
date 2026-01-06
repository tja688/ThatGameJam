# UIKit Rules Summary

Sources:
- `Assets/QFramework/QFramework_Docs_API.md` (API Group: 08.UIKit)
- `Assets/QFramework/QFramework_Docs_用户手册.md` (UIKit chapters and solutions)
- `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/3. 工具篇：QFramework.Toolkits/06. UIKit 界面管理&快速开发解决方案.md`
- `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/4. 解决方案篇/02. UIKit：界面管理&快速开发解决方案/*.md`

Key concepts:
- UIKit.Root, UIKit.Stack, UIPanel lifecycle.
- Open/close/show/hide panels via UIKit APIs.
- Panel stack navigation (Push/Back).
- Panel codegen and binding; keep logic out of generated designer files.
- Default panel loader uses ResKit; custom loaders can be set via `UIKit.Config.PanelLoaderPool`.

Verification keywords:
- "UIKit.OpenPanel"
- "UIKit.ClosePanel"
- "OpenPanelAsync"
- "UIPanel"
- "UIRoot"
- "UILevel"
- "UIPanel lifecycle"
- "PanelLoaderPool"
