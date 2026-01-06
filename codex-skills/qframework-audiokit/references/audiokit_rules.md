# AudioKit Rules Summary

Sources:
- `Assets/QFramework/QFramework_Docs_API.md` (API Group: 09.AudioKit)
- `Assets/QFramework/QFramework_Docs_用户手册.md` (AudioKit solution chapters)
- `Assets/QFramework/Toolkits/_CoreKit/Internal/Guidline/Editor/Resources/EditorGuideline/4. 解决方案篇/03. AudioKit 音频管理解决方案/*.md`

Key concepts:
- Audio channels: Music, Sound, Voice.
- Settings via `AudioKit.Settings` (toggles and volume bindables).
- Playback APIs for play, pause, resume, stop.
- Custom loader via `AudioKit.Config.AudioLoaderPool` (default uses ResKit).
- ActionKit integration for sequencing audio actions.

Verification keywords:
- "AudioKit.PlayMusic"
- "AudioKit.PlaySound"
- "AudioKit.PlayVoice"
- "AudioKit.Settings"
- "AudioLoaderPool"
- "ResKitAudioLoaderPool"
