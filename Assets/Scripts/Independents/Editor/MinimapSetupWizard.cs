// Assets/Editor/MinimapSetupWizard.cs
// Unity 6.3 LTS compatible (URP). Designed to avoid version landmines by using SerializedObject + reflection for optional parts.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class MinimapSetupWizard
{
    private const string RootFolder = "Assets/MinimapAuto";
    private const string LayerGeo = "Minimap_Geo";
    private const string LayerIcons = "Minimap_Icons";

    private const string ShaderPath = RootFolder + "/MinimapSolidColor.shader";
    private const string MatPath = RootFolder + "/MAT_MinimapSolid.mat";
    private const string RtPath = RootFolder + "/RT_Minimap.renderTexture";
    private const string RendererDataPath = RootFolder + "/URP_MinimapRenderer.asset";

    [MenuItem("Tools/Minimap/Setup (One Click)")]
    public static void SetupOneClick()
    {
        try
        {
            EnsureFolder(RootFolder);

            // 1) Ensure layers
            EnsureUserLayer(LayerGeo);
            EnsureUserLayer(LayerIcons);

            // 2) Create/ensure shader + material + RT
            EnsureShaderFile();
            var minimapMat = EnsureMaterial();
            var rt = EnsureRenderTexture(512);

            // 3) Ensure renderer data (UniversalRendererData + RenderObjects feature with override material)
            var rendererData = EnsureMinimapRendererData(minimapMat);

            // 4) Add renderer data to current URP asset renderer list
            var urpAsset = GetCurrentURPAsset();
            if (urpAsset == null)
            {
                EditorUtility.DisplayDialog("Minimap Setup",
                    "当前 Graphics Settings 没有设置 UniversalRenderPipelineAsset。\n" +
                    "请先在 Project Settings > Graphics 中设置 URP Asset，然后再运行此工具。", "OK");
                return;
            }

            int rendererIndex = EnsureRendererDataInURPAsset(urpAsset, rendererData);

            // 5) Scene objects: camera + UI
            var cam = EnsureMinimapCamera(rt, rendererIndex);
            EnsureMinimapUI(rt);

            // 6) Optional Cinemachine Brain (reflection, no compile dependency)
            TryAddCinemachineBrain(cam.gameObject);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Minimap Setup",
                "已完成 Minimap 基础搭建：\n" +
                "✅ 资产（RT/Shader/Mat/RendererData）已创建\n" +
                "✅ URP Asset Renderer List 已加入 MinimapRenderer\n" +
                "✅ 场景中已创建 MinimapCamera + UI RawImage\n\n" +
                "接下来你需要手动：\n" +
                "1) 将关卡平台/墙体等设置 Layer = Minimap_Geo\n" +
                "2) 给玩家/关键点挂 SpriteRenderer 并设 Layer = Minimap_Icons（用颜色区分）\n" +
                "3) 调 MinimapCamera 的 Orthographic Size（地图显示范围）", "OK");
        }
        catch (Exception e)
        {
            Debug.LogError("[MinimapSetupWizard] Failed:\n" + e);
            EditorUtility.DisplayDialog("Minimap Setup - Error",
                "执行失败，详情看 Console。\n" +
                "常见原因：URP 版本/资产结构被自定义、或 URP Asset 不可写。", "OK");
        }
    }

    // -----------------------------
    // Assets
    // -----------------------------

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var folder = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || parent == "Assets")
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + folder))
                AssetDatabase.CreateFolder("Assets", folder);
        }
        else
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void EnsureShaderFile()
    {
        if (File.Exists(ShaderPath)) return;

        var shaderText = @"Shader ""Minimap/SolidColorSprite""
{
    Properties
    {
        _Color (""Color"", Color) = (0.2, 0.8, 0.9, 1)
        _MainTex (""Sprite Texture"", 2D) = ""white"" {}
    }
    SubShader
    {
        Tags
        {
            ""RenderType""=""Transparent""
            ""Queue""=""Transparent""
            ""RenderPipeline""=""UniversalPipeline""
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name ""Universal2D""
            Tags { ""LightMode""=""Universal2D"" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                half4 c = _Color;
                c.a *= a * i.color.a;
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name ""UniversalForward""
            Tags { ""LightMode""=""UniversalForward"" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                half4 c = _Color;
                c.a *= a * i.color.a;
                return c;
            }
            ENDHLSL
        }
    }
}";
        File.WriteAllText(ShaderPath, shaderText);
        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);
    }

    private static Material EnsureMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat != null) return mat;

        var shader = Shader.Find("Minimap/SolidColorSprite");
        if (shader == null)
        {
            // In case import is delayed
            AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);
            shader = Shader.Find("Minimap/SolidColorSprite");
        }

        if (shader == null)
            throw new Exception("Shader 'Minimap/SolidColorSprite' not found. Ensure shader file compiled correctly.");

        mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, MatPath);
        AssetDatabase.ImportAsset(MatPath, ImportAssetOptions.ForceUpdate);
        return mat;
    }

    private static RenderTexture EnsureRenderTexture(int size)
    {
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RtPath);
        if (rt != null) return rt;

        rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32)
        {
            name = "RT_Minimap",
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };
        AssetDatabase.CreateAsset(rt, RtPath);
        AssetDatabase.ImportAsset(RtPath, ImportAssetOptions.ForceUpdate);
        return rt;
    }

    private static UniversalRendererData EnsureMinimapRendererData(Material overrideMat)
    {
        var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
        if (rd == null)
        {
            rd = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rd, RendererDataPath);
            AssetDatabase.ImportAsset(RendererDataPath, ImportAssetOptions.ForceUpdate);
        }

        // Ensure it contains a RenderObjects feature configured for Minimap_Geo
        EnsureRenderObjectsFeature(rd, overrideMat);

        EditorUtility.SetDirty(rd);
        return rd;
    }

    private static void EnsureRenderObjectsFeature(UniversalRendererData rd, Material overrideMat)
    {
        // Try to find an existing RenderObjects feature we created before
        var existing = rd.rendererFeatures.FirstOrDefault(f => f != null && f.name == "Minimap_RenderObjects");
        if (existing == null)
        {
            // Create a new RenderObjects feature (URP class)
            var roType = typeof(ScriptableRendererFeature).Assembly.GetType("UnityEngine.Rendering.Universal.RenderObjects");
            if (roType == null)
                throw new Exception("URP RenderObjects feature type not found. Is URP installed and up to date?");

            existing = ScriptableObject.CreateInstance(roType) as ScriptableRendererFeature;
            existing.name = "Minimap_RenderObjects";

            rd.rendererFeatures.Add(existing);
            rd.SetDirty();
        }

        // Configure settings using reflection to avoid minor URP version differences.
        ConfigureRenderObjectsFeature(existing, overrideMat, LayerMask.GetMask(LayerGeo));
    }

    private static void ConfigureRenderObjectsFeature(ScriptableRendererFeature feature, Material overrideMat, int layerMask)
    {
        var t = feature.GetType();

        // Many URP versions have a field named "settings" (RenderObjects.RenderObjectsSettings)
        // We'll set: filterSettings.LayerMask, overrides.overrideMaterial, overrides.overrideMaterialPassIndex, overrides.overrideDepthState etc.
        var settingsField = t.GetField("settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (settingsField == null)
            throw new Exception("RenderObjects feature 'settings' field not found. URP API might have changed.");

        object settings = settingsField.GetValue(feature);
        if (settings == null)
        {
            settings = Activator.CreateInstance(settingsField.FieldType);
            settingsField.SetValue(feature, settings);
        }

        // Set layer mask
        SetNestedValue(settings, "filterSettings", "LayerMask", layerMask);

        // Enable override material and assign
        SetNestedValue(settings, "overrideMaterial", null, overrideMat);
        // Some URP versions use "overrides" sub-struct; attempt both.
        SetNestedValue(settings, "overrides", "overrideMaterial", overrideMat);

        // Ensure override material is actually used:
        // Many versions use bool "overrideMaterial" or separate bool inside overrides.
        TrySetBool(settings, "overrideMaterial", true);
        TrySetNestedBool(settings, "overrides", "overrideMaterial", true); // sometimes it's a Material, ignore if fails.

        // Render queue type sometimes exists; we can leave defaults. But we do attempt to include transparents.
        // Set pass event to AfterRenderingTransparents when available.
        TrySetEnum(settings, "Event", "AfterRenderingTransparents");
        TrySetEnum(settings, "event", "AfterRenderingTransparents");

        EditorUtility.SetDirty(feature);
    }

    private static void SetNestedValue(object root, string fieldName, string nestedFieldName, object value)
    {
        if (root == null) return;
        var rootType = root.GetType();
        var f = rootType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return;

        object nested = f.GetValue(root);
        if (nested == null)
        {
            nested = Activator.CreateInstance(f.FieldType);
            f.SetValue(root, nested);
        }

        if (string.IsNullOrEmpty(nestedFieldName))
        {
            f.SetValue(root, value);
            return;
        }

        var nf = f.FieldType.GetField(nestedFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (nf == null) return;

        // If nested is a struct, modify copy then write back
        object boxed = nested;
        nf.SetValue(boxed, value);
        f.SetValue(root, boxed);
    }

    private static void TrySetBool(object root, string fieldName, bool value)
    {
        var f = root.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || f.FieldType != typeof(bool)) return;

        // struct copy safe
        f.SetValue(root, value);
    }

    private static void TrySetNestedBool(object root, string fieldName, string nestedBoolName, bool value)
    {
        var f = root.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return;

        object nested = f.GetValue(root);
        if (nested == null) return;

        var nf = nested.GetType().GetField(nestedBoolName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (nf == null || nf.FieldType != typeof(bool)) return;

        object boxed = nested;
        nf.SetValue(boxed, value);
        f.SetValue(root, boxed);
    }

    private static void TrySetEnum(object root, string fieldName, string enumName)
    {
        var f = root.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null || !f.FieldType.IsEnum) return;

        try
        {
            var val = Enum.Parse(f.FieldType, enumName);
            f.SetValue(root, val);
        }
        catch { /* ignore */ }
    }

    private static UniversalRenderPipelineAsset GetCurrentURPAsset()
    {
        return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
    }

    private static int EnsureRendererDataInURPAsset(UniversalRenderPipelineAsset urpAsset, UniversalRendererData rendererData)
    {
        // Add rendererData to URP Asset's renderer list via SerializedObject to avoid API drift.
        var so = new SerializedObject(urpAsset);

        // Most URP versions use m_RendererDataList
        SerializedProperty listProp = so.FindProperty("m_RendererDataList");
        if (listProp == null)
        {
            // fallback: try other known names
            listProp = so.GetIterator();
            while (listProp.NextVisible(true))
            {
                if (listProp.name.Contains("RendererDataList", StringComparison.OrdinalIgnoreCase))
                    break;
            }
        }

        if (listProp == null || !listProp.isArray)
        {
            Debug.LogWarning("[MinimapSetupWizard] Cannot find URP renderer data list in the pipeline asset. " +
                             "You may need to manually add 'URP_MinimapRenderer.asset' to the URP Asset Renderer List.");
            return 0; // fallback index
        }

        // Check if already present
        int existingIndex = -1;
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elem = listProp.GetArrayElementAtIndex(i);
            if (elem.objectReferenceValue == rendererData)
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            return existingIndex;
        }

        // Insert at end
        int newIndex = listProp.arraySize;
        listProp.InsertArrayElementAtIndex(newIndex);
        listProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = rendererData;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(urpAsset);
        AssetDatabase.SaveAssets();
        return newIndex;
    }

    // -----------------------------
    // Scene objects
    // -----------------------------

    private static Camera EnsureMinimapCamera(RenderTexture rt, int rendererIndex)
    {
        var go = GameObject.Find("MinimapCamera");
        if (go == null)
        {
            go = new GameObject("MinimapCamera");
            go.transform.position = new Vector3(0, 0, -10);
        }

        var cam = go.GetComponent<Camera>();
        if (cam == null) cam = go.AddComponent<Camera>();

        cam.orthographic = true;
        cam.orthographicSize = 12f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.targetTexture = rt;
        cam.cullingMask = LayerMask.GetMask(LayerGeo, LayerIcons);
        cam.allowHDR = false;
        cam.allowMSAA = false;

        // Set URP camera renderer index
        var addData = go.GetComponent<UniversalAdditionalCameraData>();
        if (addData == null) addData = go.AddComponent<UniversalAdditionalCameraData>();

        // In URP: int rendererIndex
        addData.SetRenderer(rendererIndex);

        EditorUtility.SetDirty(go);
        return cam;
    }

    private static void EnsureMinimapUI(RenderTexture rt)
    {
        // Find or create canvas
        Canvas canvas = GameObject.FindObjectsOfType<Canvas>().FirstOrDefault(c => c.name == "MinimapCanvas");
        if (canvas == null)
        {
            var cgo = new GameObject("MinimapCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        // Find or create RawImage
        var raw = canvas.GetComponentsInChildren<RawImage>(true).FirstOrDefault(r => r.name == "UI_Minimap");
        if (raw == null)
        {
            var rgo = new GameObject("UI_Minimap");
            rgo.transform.SetParent(canvas.transform, false);
            raw = rgo.AddComponent<RawImage>();

            var rtRect = raw.rectTransform;
            rtRect.anchorMin = new Vector2(1, 1);
            rtRect.anchorMax = new Vector2(1, 1);
            rtRect.pivot = new Vector2(1, 1);
            rtRect.anchoredPosition = new Vector2(-20, -20);
            rtRect.sizeDelta = new Vector2(220, 220);
        }

        raw.texture = rt;
        raw.color = Color.white;
        EditorUtility.SetDirty(raw);
    }

    // -----------------------------
    // Layers
    // -----------------------------

    private static void EnsureUserLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) != -1) return;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("layers");

        // Unity user layers are usually 8..31
        for (int i = 8; i <= 31; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (sp.stringValue == layerName) return;
        }

        for (int i = 8; i <= 31; i++)
        {
            var sp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }

        Debug.LogWarning($"[MinimapSetupWizard] No empty user layer slots available for '{layerName}'. Please add it manually.");
    }

    // -----------------------------
    // Optional Cinemachine
    // -----------------------------

    private static void TryAddCinemachineBrain(GameObject cameraGO)
    {
        // Add CinemachineBrain if Cinemachine is installed, without compile-time dependency.
        // Type name in package: Cinemachine.CinemachineBrain, assembly: Cinemachine
        var brainType = Type.GetType("Cinemachine.CinemachineBrain, Cinemachine");
        if (brainType == null) return;

        if (cameraGO.GetComponent(brainType) == null)
        {
            cameraGO.AddComponent(brainType);
            Debug.Log("[MinimapSetupWizard] CinemachineBrain added to MinimapCamera (Cinemachine detected).");
        }
    }
}
