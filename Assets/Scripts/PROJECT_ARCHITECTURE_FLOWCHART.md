```mermaid 

flowchart LR
%% Auto-generated architecture map
classDef model fill:#E7F5FF,stroke:#2B6CB0,stroke-width:1px;
classDef controller fill:#FFF3BF,stroke:#8A6D3B,stroke-width:1px;
classDef system fill:#E6FCF5,stroke:#087F5B,stroke-width:1px;
classDef view fill:#FDE2E4,stroke:#C2255C,stroke-width:1px;
classDef event fill:#FFE8CC,stroke:#D9480F,stroke-width:1px;
classDef command fill:#EDE7F6,stroke:#5F3DC4,stroke-width:1px;
classDef query fill:#E3FAFC,stroke:#0C8599,stroke-width:1px;
classDef externalIn fill:#F1F3F5,stroke:#868E96,stroke-width:1px;
classDef externalOut fill:#F8F0FC,stroke:#9C36B5,stroke-width:1px;
classDef other fill:#F8F9FA,stroke:#495057,stroke-width:1px;

subgraph ExternalInput["External Input（外部输入）"]
  EI_Update["Update（Unity 每帧更新）"]:::externalIn
  EI_OnTrigger2D["OnTriggerEnter2D/Exit2D（2D 触发）"]:::externalIn
  EI_InputGetKeyDown["Input.GetKeyDown（键盘输入）"]:::externalIn
  EI_FixedUpdate["FixedUpdate（Unity 固定帧更新）"]:::externalIn
  EI_InputAction["InputAction（输入系统）"]:::externalIn
  EI_RuntimeInitializeOnLoadMethod["RuntimeInitializeOnLoadMethod（启动回调）"]:::externalIn
end

subgraph SharedKernel["Shared Kernel（跨 Feature 共享内核）"]
  subgraph Shared_Event["Events（事件）"]
    N4_DarknessStateChangedEvent["DarknessStateChangedEvent（黑暗状态变化事件）"]:::event
    N9_DeathCountChangedEvent["DeathCountChangedEvent（死亡数量变化事件）"]:::event
    N38_LampCountChangedEvent["LampCountChangedEvent（灯数量变化事件）"]:::event
    N39_LampSpawnedEvent["LampSpawnedEvent（灯生成事件）"]:::event
    N40_LightChangedEvent["LightChangedEvent（光变化事件）"]:::event
    N41_LightConsumedEvent["LightConsumedEvent（光消耗事件）"]:::event
    N42_LightDepletedEvent["LightDepletedEvent（光耗尽事件）"]:::event
    N58_PlayerDiedEvent["PlayerDiedEvent（玩家死亡事件）"]:::event
    N61_PlayerRespawnedEvent["PlayerRespawnedEvent（玩家重生事件）"]:::event
    N73_RunFailedEvent["RunFailedEvent（本局失败事件）"]:::event
    N75_RunResetEvent["RunResetEvent（本局重置事件）"]:::event
    N78_SafeZoneStateChangedEvent["SafeZoneStateChangedEvent（安全区域状态变化事件）"]:::event
  end
  subgraph Shared_Utility["Utility（工具）"]
    N12_EDeathReason["EDeathReason（死亡原因枚举）"]:::other
    N13_ELightConsumeReason["ELightConsumeReason（光消耗原因枚举）"]:::other
  end
end

subgraph Controller["Controllers（控制器）"]
  subgraph Controller_Darkness["Darkness（黑暗）"]
    N6_DarknessTickController["DarknessTickController（黑暗帧更新控制器）"]:::controller
    N7_DarknessZone2D["DarknessZone2D（黑暗区域2D）"]:::controller
    N57_PlayerDarknessSensor["PlayerDarknessSensor（玩家黑暗感知器）"]:::controller
  end
  subgraph Controller_DeathRespawn["DeathRespawn（死亡重生）"]
    N8_DeathController["DeathController（死亡控制器）"]:::controller
    N37_KillVolume2D["KillVolume2D（击杀区域2D）"]:::controller
    N68_RespawnController["RespawnController（重生控制器）"]:::controller
  end
  subgraph Controller_KeroseneLamp["KeroseneLamp（煤油灯）"]
    N35_KeroseneLampManager["KeroseneLampManager（煤油灯管理器）"]:::controller
  end
  subgraph Controller_LightVitality["LightVitality（光生命力）"]
    N44_LightVitalityDebugController["LightVitalityDebugController（光生命力调试控制器）"]:::controller
    N46_LightVitalityResetController["LightVitalityResetController（光生命力重置控制器）"]:::controller
  end
  subgraph Controller_PlayerCharacter2D["PlayerCharacter2D（玩家角色2D）"]
    N51_PlatformerCharacterController["PlatformerCharacterController（平台跳跃角色控制器）"]:::controller
    N52_PlatformerCharacterInput["PlatformerCharacterInput（平台跳跃角色输入）"]:::controller
  end
  subgraph Controller_RunFailHandling["RunFailHandling（本局失败处理）"]
    N69_RunFailHandlingController["RunFailHandlingController（本局失败处理控制器）"]:::controller
  end
  subgraph Controller_RunFailReset["RunFailReset（本局失败重置）"]
    N72_RunFailSettingsController["RunFailSettingsController（本局失败设置控制器）"]:::controller
  end
  subgraph Controller_SafeZone["SafeZone（安全区域）"]
    N62_PlayerSafeZoneSensor["PlayerSafeZoneSensor（玩家安全区域感知器）"]:::controller
    N76_SafeZone2D["SafeZone2D（安全区域2D）"]:::controller
    N80_SafeZoneTickController["SafeZoneTickController（安全区域帧更新控制器）"]:::controller
  end
  subgraph Controller_Test["Test（Test）"]
    N88_SimplePlayerController["SimplePlayerController（简单玩家控制器）"]:::controller
    N89_SpineColorController["SpineColorController（Spine颜色控制器）"]:::controller
  end
  subgraph Controller_Testing["Testing（Testing）"]
    N74_RunResetController["RunResetController（本局重置控制器）"]:::controller
  end
end

subgraph Command["Commands（命令）"]
  subgraph Command_Darkness["Darkness（黑暗）"]
    N83_SetInDarknessCommand["SetInDarknessCommand（设置处于黑暗命令）"]:::command
  end
  subgraph Command_DeathRespawn["DeathRespawn（死亡重生）"]
    N48_MarkPlayerDeadCommand["MarkPlayerDeadCommand（标记玩家Dead命令）"]:::command
    N49_MarkPlayerRespawnedCommand["MarkPlayerRespawnedCommand（标记玩家重生命令）"]:::command
    N65_ResetDeathCountCommand["ResetDeathCountCommand（重置死亡数量命令）"]:::command
  end
  subgraph Command_KeroseneLamp["KeroseneLamp（煤油灯）"]
    N64_RecordLampSpawnedCommand["RecordLampSpawnedCommand（记录灯生成命令）"]:::command
    N66_ResetLampsCommand["ResetLampsCommand（重置Lamps命令）"]:::command
  end
  subgraph Command_LightVitality["LightVitality（光生命力）"]
    N1_AddLightCommand["AddLightCommand（增加光命令）"]:::command
    N2_ConsumeLightCommand["ConsumeLightCommand（消耗光命令）"]:::command
    N84_SetLightCommand["SetLightCommand（设置光命令）"]:::command
    N86_SetMaxLightCommand["SetMaxLightCommand（设置最大光命令）"]:::command
  end
  subgraph Command_PlayerCharacter2D["PlayerCharacter2D（玩家角色2D）"]
    N82_SetFrameInputCommand["SetFrameInputCommand（设置帧输入命令）"]:::command
    N90_TickFixedStepCommand["TickFixedStepCommand（帧更新固定步命令）"]:::command
  end
  subgraph Command_RunFailReset["RunFailReset（本局失败重置）"]
    N50_MarkRunFailedCommand["MarkRunFailedCommand（标记本局失败命令）"]:::command
    N67_ResetRunCommand["ResetRunCommand（重置本局命令）"]:::command
    N85_SetMaxDeathsCommand["SetMaxDeathsCommand（设置最大死亡数命令）"]:::command
  end
  subgraph Command_SafeZone["SafeZone（安全区域）"]
    N87_SetSafeZoneCountCommand["SetSafeZoneCountCommand（设置安全区域数量命令）"]:::command
  end
end

subgraph System["Systems（系统）"]
  subgraph System_Darkness["Darkness（黑暗）"]
    N5_DarknessSystem["DarknessSystem（黑暗系统）"]:::system
    N22_IDarknessSystem["IDarknessSystem（黑暗系统接口）"]:::system
  end
  subgraph System_DeathRespawn["DeathRespawn（死亡重生）"]
    N11_DeathRespawnSystem["DeathRespawnSystem（死亡重生系统）"]:::system
    N24_IDeathRespawnSystem["IDeathRespawnSystem（死亡重生系统接口）"]:::system
  end
  subgraph System_LightVitality["LightVitality（光生命力）"]
    N27_ILightVitalityResetSystem["ILightVitalityResetSystem（光生命力重置系统接口）"]:::system
    N47_LightVitalityResetSystem["LightVitalityResetSystem（光生命力重置系统）"]:::system
  end
  subgraph System_PlayerCharacter2D["PlayerCharacter2D（玩家角色2D）"]
    N30_IPlayerCharacter2DSystem["IPlayerCharacter2DSystem（玩家角色2D系统接口）"]:::system
    N56_PlayerCharacter2DSystem["PlayerCharacter2DSystem（玩家角色2D系统）"]:::system
  end
  subgraph System_RunFailReset["RunFailReset（本局失败重置）"]
    N32_IRunFailResetSystem["IRunFailResetSystem（本局失败重置系统接口）"]:::system
    N71_RunFailResetSystem["RunFailResetSystem（本局失败重置系统）"]:::system
  end
  subgraph System_SafeZone["SafeZone（安全区域）"]
    N34_ISafeZoneSystem["ISafeZoneSystem（安全区域系统接口）"]:::system
    N79_SafeZoneSystem["SafeZoneSystem（安全区域系统）"]:::system
  end
end

subgraph Model["Models（模型）"]
  subgraph Model_Darkness["Darkness（黑暗）"]
    N3_DarknessModel["DarknessModel（黑暗模型★唯一事实来源）"]:::model
    N21_IDarknessModel["IDarknessModel（黑暗模型接口）"]:::model
  end
  subgraph Model_DeathRespawn["DeathRespawn（死亡重生）"]
    N10_DeathRespawnModel["DeathRespawnModel（死亡重生模型★唯一事实来源）"]:::model
    N23_IDeathRespawnModel["IDeathRespawnModel（死亡重生模型接口）"]:::model
  end
  subgraph Model_KeroseneLamp["KeroseneLamp（煤油灯）"]
    N25_IKeroseneLampModel["IKeroseneLampModel（煤油灯模型接口）"]:::model
    N36_KeroseneLampModel["KeroseneLampModel（煤油灯模型★唯一事实来源）"]:::model
  end
  subgraph Model_LightVitality["LightVitality（光生命力）"]
    N26_ILightVitalityModel["ILightVitalityModel（光生命力模型接口）"]:::model
    N45_LightVitalityModel["LightVitalityModel（光生命力模型★唯一事实来源）"]:::model
  end
  subgraph Model_PlayerCharacter2D["PlayerCharacter2D（玩家角色2D）"]
    N28_IPlatformerFrameInputSource["IPlatformerFrameInputSource（平台跳跃帧输入来源接口）"]:::model
    N29_IPlayerCharacter2DModel["IPlayerCharacter2DModel（玩家角色2D模型接口）"]:::model
    N54_PlatformerFrameInput["PlatformerFrameInput（平台跳跃帧输入）"]:::model
    N55_PlayerCharacter2DModel["PlayerCharacter2DModel（玩家角色2D模型★唯一事实来源）"]:::model
  end
  subgraph Model_RunFailReset["RunFailReset（本局失败重置）"]
    N31_IRunFailResetModel["IRunFailResetModel（本局失败重置模型接口）"]:::model
    N70_RunFailResetModel["RunFailResetModel（本局失败重置模型★唯一事实来源）"]:::model
  end
  subgraph Model_SafeZone["SafeZone（安全区域）"]
    N33_ISafeZoneModel["ISafeZoneModel（安全区域模型接口）"]:::model
    N77_SafeZoneModel["SafeZoneModel（安全区域模型★唯一事实来源）"]:::model
  end
end

subgraph Query["Queries（查询）"]
  subgraph Query_LightVitality["LightVitality（光生命力）"]
    N16_GetCurrentLightQuery["GetCurrentLightQuery（获取当前光查询）"]:::query
    N18_GetLightPercentQuery["GetLightPercentQuery（获取光百分比查询）"]:::query
    N19_GetMaxLightQuery["GetMaxLightQuery（获取最大光查询）"]:::query
  end
  subgraph Query_PlayerCharacter2D["PlayerCharacter2D（玩家角色2D）"]
    N17_GetDesiredVelocityQuery["GetDesiredVelocityQuery（获取期望速度查询）"]:::query
  end
end

subgraph Event["Events（事件）"]
  subgraph Event_PlayerCharacter2D["PlayerCharacter2D（玩家角色2D）"]
    N59_PlayerGroundedChangedEvent["PlayerGroundedChangedEvent（玩家落地变化事件）"]:::event
    N60_PlayerJumpedEvent["PlayerJumpedEvent（玩家跳跃事件）"]:::event
  end
end

subgraph View["Views/Outputs（视图/输出）"]
  subgraph View_HUD["HUD（抬头显示）"]
    N20_HUDController["HUDController（抬头显示控制器）"]:::view
  end
  subgraph View_Test["Test（Test）"]
    N14_FeaturePrototypeRelayPanel["FeaturePrototypeRelayPanel（功能原型转发面板）"]:::view
  end
end

subgraph Other["Other（其他/未归类）"]
  subgraph Other_Root["Root（根）"]
    N15_GameRootApp["GameRootApp（游戏根应用）"]:::other
    N63_ProjectToolkitBootstrap["ProjectToolkitBootstrap（项目工具包启动）"]:::other
  end
  subgraph Other_Test["Test（Test）"]
    N81_SceneReloader["SceneReloader（场景重载器）"]:::other
  end
end

subgraph Utility["Other（其他/工具）"]
  subgraph Utility_LightVitality["LightVitality（光生命力）"]
    N43_LightVitalityCommandUtils["LightVitalityCommandUtils（光生命力命令工具）"]:::other
  end
  subgraph Utility_PlayerCharacter2D["PlayerCharacter2D（玩家角色2D）"]
    N53_PlatformerCharacterStats["PlatformerCharacterStats（平台跳跃角色参数）"]:::other
  end
end

subgraph ExternalOutput["External Output（对外输出）"]
  EO_Rigidbody2D_linearVelocity["Rigidbody2D.linearVelocity（物理速度）"]:::externalOut
  EO_Text_text["Text.text（UI 文本）"]:::externalOut
  EO_Image_fillAmount["Image.fillAmount（UI 填充）"]:::externalOut
  EO_GameObject_SetActive["GameObject.SetActive（UI 显示）"]:::externalOut
  EO_LogKit_W["LogKit.W（日志警告）"]:::externalOut
  EO_Instantiate["Instantiate（实例化）"]:::externalOut
  EO_Destroy["Destroy（销毁）"]:::externalOut
  EO_Debug_Log["Debug.Log（日志输出）"]:::externalOut
  EO_TMP_Text_text["TMP_Text.text（UI 文本）"]:::externalOut
  EO_SceneManager_LoadScene["SceneManager.LoadScene（场景重载）"]:::externalOut
  EO_Transform_localScale["Transform.localScale（朝向缩放）"]:::externalOut
  EO_Skeleton_RGBA["Skeleton.RGBA（Spine 渲染）"]:::externalOut
end

N1_AddLightCommand -->|write| N26_ILightVitalityModel
N1_AddLightCommand -->|use| N43_LightVitalityCommandUtils
N2_ConsumeLightCommand -->|use| N13_ELightConsumeReason
N2_ConsumeLightCommand -->|write| N26_ILightVitalityModel
N2_ConsumeLightCommand -->|publish| N41_LightConsumedEvent
N2_ConsumeLightCommand -->|use| N43_LightVitalityCommandUtils
N5_DarknessSystem -->|dispatch| N2_ConsumeLightCommand
N5_DarknessSystem -->|use| N13_ELightConsumeReason
N5_DarknessSystem -->|read| N21_IDarknessModel
N5_DarknessSystem -->|subscribe| N78_SafeZoneStateChangedEvent
N6_DarknessTickController -->|use| N15_GameRootApp
N6_DarknessTickController -->|call| N22_IDarknessSystem
N7_DarknessZone2D -->|find| N57_PlayerDarknessSensor
N8_DeathController -->|use| N12_EDeathReason
N8_DeathController -->|use| N15_GameRootApp
N8_DeathController -->|read| N23_IDeathRespawnModel
N8_DeathController -->|call| N24_IDeathRespawnSystem
N8_DeathController -->|subscribe| N42_LightDepletedEvent
N11_DeathRespawnSystem -->|use| N12_EDeathReason
N11_DeathRespawnSystem -->|dispatch| N48_MarkPlayerDeadCommand
N11_DeathRespawnSystem -->|dispatch| N49_MarkPlayerRespawnedCommand
EI_FixedUpdate -->|input| N51_PlatformerCharacterController
EI_FixedUpdate -->|input| N88_SimplePlayerController
EI_InputAction -->|input| N52_PlatformerCharacterInput
EI_InputAction -->|input| N88_SimplePlayerController
EI_InputGetKeyDown -->|input| N14_FeaturePrototypeRelayPanel
EI_InputGetKeyDown -->|input| N44_LightVitalityDebugController
EI_InputGetKeyDown -->|input| N74_RunResetController
EI_InputGetKeyDown -->|input| N81_SceneReloader
EI_OnTrigger2D -->|input| N7_DarknessZone2D
EI_OnTrigger2D -->|input| N37_KillVolume2D
EI_OnTrigger2D -->|input| N76_SafeZone2D
EI_RuntimeInitializeOnLoadMethod -->|input| N63_ProjectToolkitBootstrap
EI_Update -->|input| N6_DarknessTickController
EI_Update -->|input| N8_DeathController
EI_Update -->|input| N14_FeaturePrototypeRelayPanel
EI_Update -->|input| N44_LightVitalityDebugController
EI_Update -->|input| N51_PlatformerCharacterController
EI_Update -->|input| N57_PlayerDarknessSensor
EI_Update -->|input| N74_RunResetController
EI_Update -->|input| N80_SafeZoneTickController
EI_Update -->|input| N81_SceneReloader
EI_Update -->|input| N88_SimplePlayerController
EI_Update -->|input| N89_SpineColorController
N14_FeaturePrototypeRelayPanel -->|dispatch| N1_AddLightCommand
N14_FeaturePrototypeRelayPanel -->|dispatch| N2_ConsumeLightCommand
N14_FeaturePrototypeRelayPanel -->|subscribe| N4_DarknessStateChangedEvent
N14_FeaturePrototypeRelayPanel -->|use| N13_ELightConsumeReason
N14_FeaturePrototypeRelayPanel -->|render| EO_TMP_Text_text
N14_FeaturePrototypeRelayPanel -->|use| N15_GameRootApp
N14_FeaturePrototypeRelayPanel -->|read| N21_IDarknessModel
N14_FeaturePrototypeRelayPanel -->|read| N26_ILightVitalityModel
N14_FeaturePrototypeRelayPanel -->|read| N33_ISafeZoneModel
N14_FeaturePrototypeRelayPanel -->|subscribe| N40_LightChangedEvent
N14_FeaturePrototypeRelayPanel -->|subscribe| N42_LightDepletedEvent
N14_FeaturePrototypeRelayPanel -->|subscribe| N78_SafeZoneStateChangedEvent
N14_FeaturePrototypeRelayPanel -->|dispatch| N83_SetInDarknessCommand
N14_FeaturePrototypeRelayPanel -->|dispatch| N84_SetLightCommand
N14_FeaturePrototypeRelayPanel -->|dispatch| N87_SetSafeZoneCountCommand
N15_GameRootApp -->|register| N3_DarknessModel
N15_GameRootApp -->|register| N5_DarknessSystem
N15_GameRootApp -->|register| N10_DeathRespawnModel
N15_GameRootApp -->|register| N11_DeathRespawnSystem
N15_GameRootApp -->|register| N21_IDarknessModel
N15_GameRootApp -->|register| N22_IDarknessSystem
N15_GameRootApp -->|register| N23_IDeathRespawnModel
N15_GameRootApp -->|register| N24_IDeathRespawnSystem
N15_GameRootApp -->|register| N25_IKeroseneLampModel
N15_GameRootApp -->|register| N26_ILightVitalityModel
N15_GameRootApp -->|register| N27_ILightVitalityResetSystem
N15_GameRootApp -->|register| N29_IPlayerCharacter2DModel
N15_GameRootApp -->|register| N30_IPlayerCharacter2DSystem
N15_GameRootApp -->|register| N31_IRunFailResetModel
N15_GameRootApp -->|register| N32_IRunFailResetSystem
N15_GameRootApp -->|register| N33_ISafeZoneModel
N15_GameRootApp -->|register| N34_ISafeZoneSystem
N15_GameRootApp -->|register| N36_KeroseneLampModel
N15_GameRootApp -->|register| N45_LightVitalityModel
N15_GameRootApp -->|register| N47_LightVitalityResetSystem
N15_GameRootApp -->|register| N55_PlayerCharacter2DModel
N15_GameRootApp -->|register| N56_PlayerCharacter2DSystem
N15_GameRootApp -->|register| N70_RunFailResetModel
N15_GameRootApp -->|register| N71_RunFailResetSystem
N15_GameRootApp -->|register| N77_SafeZoneModel
N15_GameRootApp -->|register| N79_SafeZoneSystem
N16_GetCurrentLightQuery -->|read| N26_ILightVitalityModel
N17_GetDesiredVelocityQuery -->|read| N29_IPlayerCharacter2DModel
N18_GetLightPercentQuery -->|read| N26_ILightVitalityModel
N19_GetMaxLightQuery -->|read| N26_ILightVitalityModel
N20_HUDController -->|render| EO_GameObject_SetActive
N20_HUDController -->|render| EO_Image_fillAmount
N20_HUDController -->|render| EO_Text_text
N20_HUDController -->|use| N15_GameRootApp
N20_HUDController -->|read| N21_IDarknessModel
N20_HUDController -->|read| N25_IKeroseneLampModel
N20_HUDController -->|read| N26_ILightVitalityModel
N20_HUDController -->|read| N31_IRunFailResetModel
N20_HUDController -->|read| N33_ISafeZoneModel
N24_IDeathRespawnSystem -->|use| N12_EDeathReason
N29_IPlayerCharacter2DModel -->|use| N54_PlatformerFrameInput
N35_KeroseneLampManager -->|output| EO_Destroy
N35_KeroseneLampManager -->|output| EO_Instantiate
N35_KeroseneLampManager -->|output| EO_LogKit_W
N35_KeroseneLampManager -->|use| N15_GameRootApp
N35_KeroseneLampManager -->|subscribe| N58_PlayerDiedEvent
N35_KeroseneLampManager -->|dispatch| N64_RecordLampSpawnedCommand
N35_KeroseneLampManager -->|dispatch| N66_ResetLampsCommand
N35_KeroseneLampManager -->|subscribe| N75_RunResetEvent
N37_KillVolume2D -->|find| N8_DeathController
N37_KillVolume2D -->|use| N12_EDeathReason
N41_LightConsumedEvent -->|use| N13_ELightConsumeReason
N43_LightVitalityCommandUtils -->|publish| N40_LightChangedEvent
N43_LightVitalityCommandUtils -->|publish| N42_LightDepletedEvent
N44_LightVitalityDebugController -->|dispatch| N1_AddLightCommand
N44_LightVitalityDebugController -->|dispatch| N2_ConsumeLightCommand
N44_LightVitalityDebugController -->|use| N13_ELightConsumeReason
N44_LightVitalityDebugController -->|output| EO_LogKit_W
N44_LightVitalityDebugController -->|use| N15_GameRootApp
N44_LightVitalityDebugController -->|read| N19_GetMaxLightQuery
N44_LightVitalityDebugController -->|subscribe| N40_LightChangedEvent
N44_LightVitalityDebugController -->|dispatch| N84_SetLightCommand
N46_LightVitalityResetController -->|use| N15_GameRootApp
N46_LightVitalityResetController -->|read| N19_GetMaxLightQuery
N46_LightVitalityResetController -->|subscribe| N61_PlayerRespawnedEvent
N46_LightVitalityResetController -->|subscribe| N75_RunResetEvent
N46_LightVitalityResetController -->|dispatch| N84_SetLightCommand
N47_LightVitalityResetSystem -->|read| N26_ILightVitalityModel
N47_LightVitalityResetSystem -->|subscribe| N61_PlayerRespawnedEvent
N47_LightVitalityResetSystem -->|subscribe| N75_RunResetEvent
N47_LightVitalityResetSystem -->|dispatch| N84_SetLightCommand
N48_MarkPlayerDeadCommand -->|publish| N9_DeathCountChangedEvent
N48_MarkPlayerDeadCommand -->|use| N12_EDeathReason
N48_MarkPlayerDeadCommand -->|write| N23_IDeathRespawnModel
N48_MarkPlayerDeadCommand -->|publish| N58_PlayerDiedEvent
N49_MarkPlayerRespawnedCommand -->|write| N23_IDeathRespawnModel
N49_MarkPlayerRespawnedCommand -->|publish| N61_PlayerRespawnedEvent
N50_MarkRunFailedCommand -->|write| N31_IRunFailResetModel
N50_MarkRunFailedCommand -->|publish| N73_RunFailedEvent
N51_PlatformerCharacterController -->|output| EO_Debug_Log
N51_PlatformerCharacterController -->|output| EO_Rigidbody2D_linearVelocity
N51_PlatformerCharacterController -->|use| N15_GameRootApp
N51_PlatformerCharacterController -->|read| N17_GetDesiredVelocityQuery
N51_PlatformerCharacterController -->|use| N28_IPlatformerFrameInputSource
N51_PlatformerCharacterController -->|read| N29_IPlayerCharacter2DModel
N51_PlatformerCharacterController -->|use| N53_PlatformerCharacterStats
N51_PlatformerCharacterController -->|use| N54_PlatformerFrameInput
N51_PlatformerCharacterController -->|subscribe| N58_PlayerDiedEvent
N51_PlatformerCharacterController -->|subscribe| N59_PlayerGroundedChangedEvent
N51_PlatformerCharacterController -->|subscribe| N60_PlayerJumpedEvent
N51_PlatformerCharacterController -->|subscribe| N61_PlayerRespawnedEvent
N51_PlatformerCharacterController -->|dispatch| N82_SetFrameInputCommand
N51_PlatformerCharacterController -->|dispatch| N90_TickFixedStepCommand
N52_PlatformerCharacterInput -->|use| N28_IPlatformerFrameInputSource
N52_PlatformerCharacterInput -->|use| N54_PlatformerFrameInput
N54_PlatformerFrameInput -->|use| N28_IPlatformerFrameInputSource
N55_PlayerCharacter2DModel -->|use| N54_PlatformerFrameInput
N57_PlayerDarknessSensor -->|find| N7_DarknessZone2D
N57_PlayerDarknessSensor -->|use| N15_GameRootApp
N57_PlayerDarknessSensor -->|subscribe| N61_PlayerRespawnedEvent
N57_PlayerDarknessSensor -->|subscribe| N75_RunResetEvent
N57_PlayerDarknessSensor -->|dispatch| N83_SetInDarknessCommand
N58_PlayerDiedEvent -->|use| N12_EDeathReason
N62_PlayerSafeZoneSensor -->|use| N15_GameRootApp
N62_PlayerSafeZoneSensor -->|subscribe| N61_PlayerRespawnedEvent
N62_PlayerSafeZoneSensor -->|subscribe| N75_RunResetEvent
N62_PlayerSafeZoneSensor -->|find| N76_SafeZone2D
N62_PlayerSafeZoneSensor -->|dispatch| N87_SetSafeZoneCountCommand
N63_ProjectToolkitBootstrap -->|use| N15_GameRootApp
N64_RecordLampSpawnedCommand -->|write| N25_IKeroseneLampModel
N64_RecordLampSpawnedCommand -->|publish| N38_LampCountChangedEvent
N64_RecordLampSpawnedCommand -->|publish| N39_LampSpawnedEvent
N65_ResetDeathCountCommand -->|publish| N9_DeathCountChangedEvent
N65_ResetDeathCountCommand -->|write| N23_IDeathRespawnModel
N66_ResetLampsCommand -->|write| N25_IKeroseneLampModel
N66_ResetLampsCommand -->|publish| N38_LampCountChangedEvent
N67_ResetRunCommand -->|write| N31_IRunFailResetModel
N68_RespawnController -->|output| EO_Rigidbody2D_linearVelocity
N68_RespawnController -->|use| N15_GameRootApp
N68_RespawnController -->|call| N24_IDeathRespawnSystem
N68_RespawnController -->|subscribe| N58_PlayerDiedEvent
N68_RespawnController -->|dispatch| N65_ResetDeathCountCommand
N68_RespawnController -->|subscribe| N75_RunResetEvent
N69_RunFailHandlingController -->|output| EO_LogKit_W
N69_RunFailHandlingController -->|use| N15_GameRootApp
N69_RunFailHandlingController -->|call| N32_IRunFailResetSystem
N69_RunFailHandlingController -->|subscribe| N73_RunFailedEvent
N71_RunFailResetSystem -->|subscribe| N9_DeathCountChangedEvent
N71_RunFailResetSystem -->|read| N31_IRunFailResetModel
N71_RunFailResetSystem -->|dispatch| N50_MarkRunFailedCommand
N71_RunFailResetSystem -->|dispatch| N67_ResetRunCommand
N71_RunFailResetSystem -->|publish| N75_RunResetEvent
N72_RunFailSettingsController -->|use| N15_GameRootApp
N72_RunFailSettingsController -->|dispatch| N85_SetMaxDeathsCommand
N74_RunResetController -->|use| N15_GameRootApp
N74_RunResetController -->|read| N31_IRunFailResetModel
N74_RunResetController -->|call| N32_IRunFailResetSystem
N74_RunResetController -->|subscribe| N73_RunFailedEvent
N74_RunResetController -->|subscribe| N75_RunResetEvent
N76_SafeZone2D -->|find| N62_PlayerSafeZoneSensor
N79_SafeZoneSystem -->|dispatch| N1_AddLightCommand
N79_SafeZoneSystem -->|read| N33_ISafeZoneModel
N80_SafeZoneTickController -->|use| N15_GameRootApp
N80_SafeZoneTickController -->|call| N34_ISafeZoneSystem
N81_SceneReloader -->|output| EO_Debug_Log
N81_SceneReloader -->|output| EO_SceneManager_LoadScene
N82_SetFrameInputCommand -->|write| N29_IPlayerCharacter2DModel
N82_SetFrameInputCommand -->|use| N54_PlatformerFrameInput
N83_SetInDarknessCommand -->|publish| N4_DarknessStateChangedEvent
N83_SetInDarknessCommand -->|write| N21_IDarknessModel
N84_SetLightCommand -->|write| N26_ILightVitalityModel
N84_SetLightCommand -->|use| N43_LightVitalityCommandUtils
N85_SetMaxDeathsCommand -->|write| N31_IRunFailResetModel
N86_SetMaxLightCommand -->|write| N26_ILightVitalityModel
N86_SetMaxLightCommand -->|publish| N40_LightChangedEvent
N86_SetMaxLightCommand -->|publish| N42_LightDepletedEvent
N87_SetSafeZoneCountCommand -->|write| N33_ISafeZoneModel
N87_SetSafeZoneCountCommand -->|publish| N78_SafeZoneStateChangedEvent
N88_SimplePlayerController -->|output| EO_Rigidbody2D_linearVelocity
N88_SimplePlayerController -->|output| EO_Transform_localScale
N89_SpineColorController -->|render| EO_Skeleton_RGBA
N90_TickFixedStepCommand -->|write| N29_IPlayerCharacter2DModel
N90_TickFixedStepCommand -->|use| N53_PlatformerCharacterStats
N90_TickFixedStepCommand -->|publish| N59_PlayerGroundedChangedEvent
N90_TickFixedStepCommand -->|publish| N60_PlayerJumpedEvent

```mermaid