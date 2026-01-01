using UnityEngine;
using UnityEngine.Rendering;

public class PaperParamSRPDriver : MonoBehaviour
{
    [Header("Assign the REAL rendering camera (the one with CinemachineBrain)")]
    public Camera targetCamera;

    [Header("Assign the SAME Material used by your Full Screen Pass Renderer Feature")]
    public Material paperMaterial;

    [Header("Write mode")]
    public bool writeToMaterial = true;   // 若你的shader把参数放在PerMaterial里，必须true
    public bool writeToGlobal   = false;  // 只有shader用全局uniform时才需要

    [Header("Debug overlay")]
    public bool showOverlay = true;

    static readonly int PaperOrthoSizeID = Shader.PropertyToID("_PaperOrthoSize");
    static readonly int PaperCamPosID    = Shader.PropertyToID("_PaperCamPos");

    Vector3 lastCamPos;
    float lastOrtho;
    Vector4 globalCamPos;
    float globalOrtho;
    Vector4 matCamPos;
    float matOrtho;
    string lastCamName = "(none)";

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (!targetCamera) return;
        if (cam != targetCamera) return;  // 只处理目标相机，避免UI/SceneView覆盖

        lastCamName = cam.name;
        lastCamPos  = cam.transform.position;
        lastOrtho   = cam.orthographicSize;

        // 写入（按你shader实际声明方式选择）
        if (writeToMaterial && paperMaterial)
        {
            if (paperMaterial.HasProperty(PaperOrthoSizeID))
                paperMaterial.SetFloat(PaperOrthoSizeID, lastOrtho);

            if (paperMaterial.HasProperty(PaperCamPosID))
                paperMaterial.SetVector(PaperCamPosID, new Vector4(lastCamPos.x, lastCamPos.y, 0f, 0f));
        }

        if (writeToGlobal)
        {
            Shader.SetGlobalFloat(PaperOrthoSizeID, lastOrtho);
            Shader.SetGlobalVector(PaperCamPosID, new Vector4(lastCamPos.x, lastCamPos.y, 0f, 0f));
        }

        // 读回（确认到底有没有写进去）
        globalOrtho  = Shader.GetGlobalFloat(PaperOrthoSizeID);
        globalCamPos = Shader.GetGlobalVector(PaperCamPosID);

        if (paperMaterial)
        {
            matOrtho  = paperMaterial.HasProperty(PaperOrthoSizeID) ? paperMaterial.GetFloat(PaperOrthoSizeID) : -999f;
            matCamPos = paperMaterial.HasProperty(PaperCamPosID) ? paperMaterial.GetVector(PaperCamPosID) : new Vector4(-999,-999,-999,-999);
        }
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        GUILayout.BeginArea(new Rect(10, 10, 760, 180), GUI.skin.box);
        GUILayout.Label($"[RenderCam] {lastCamName} pos=({lastCamPos.x:F3},{lastCamPos.y:F3},{lastCamPos.z:F3}) ortho={lastOrtho:F3}");
        GUILayout.Label($"[Global] _PaperCamPos=({globalCamPos.x:F3},{globalCamPos.y:F3}) _PaperOrthoSize={globalOrtho:F3}");
        if (paperMaterial)
            GUILayout.Label($"[Material] _PaperCamPos=({matCamPos.x:F3},{matCamPos.y:F3}) _PaperOrthoSize={matOrtho:F3}");
        else
            GUILayout.Label("[Material] (paperMaterial not assigned)");
        GUILayout.Label($"writeToMaterial={writeToMaterial} writeToGlobal={writeToGlobal}");
        GUILayout.EndArea();
    }
}
