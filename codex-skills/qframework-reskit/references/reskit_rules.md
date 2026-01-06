# ResKit Rules Summary

Sources:
- `Assets/QFramework/QFramework_Docs_API.md` (API Group: 07.ResKit)
- `Assets/QFramework/QFramework_Docs_用户手册.md` (ResKit solution chapters)
- `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/4. 解决方案篇/01. ResKit：资源管理&开发解决方案/*.md`

Key concepts:
- Initialize ResKit when required (Init or InitAsync).
- Use ResLoader per loading unit; recycle with Recycle2Cache when done.
- Load assets sync/async via ResLoader; support scene loading APIs.
- AssetBundle marking and simulation mode for development.

Verification keywords:
- "ResKit.Init"
- "ResKit.InitAsync"
- "ResLoader.Allocate"
- "Recycle2Cache"
- "Add2Load"
- "LoadSync"
- "LoadAsync"
- "LoadSceneSync"
- "LoadSceneAsync"
- "@ResKit- AssetBundle Mark"
