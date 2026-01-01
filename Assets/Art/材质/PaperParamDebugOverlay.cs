using UnityEngine;

[DefaultExecutionOrder(2000)] // 尽量在Cinemachine之后
public class PaperParamDebugOverlay : MonoBehaviour
{
    public Camera targetCamera;
    [Tooltip("把 Full Screen Pass 里用的那个 Material 拖进来（如果你用的是 material.Set... 路线）")]
    public Material targetMaterial;

    static readonly int PaperOrthoSizeID = Shader.PropertyToID("_PaperOrthoSize");
    static readonly int PaperCamPosID    = Shader.PropertyToID("_PaperCamPos");

    Vector2 camPosXY;
    float orthoSize;

    Vector4 globalCamPos;
    float globalOrtho;

    Vector4 matCamPos;
    float matOrtho;

    void Awake()
    {
        if (!targetCamera) targetCamera = GetComponent<Camera>();
    }

    void OnPreRender()
    {
        if (!targetCamera) return;

        orthoSize = targetCamera.orthographicSize;
        Vector3 p = targetCamera.transform.position;
        camPosXY = new Vector2(p.x, p.y);

        // 读全局（如果你用Shader.SetGlobal...）
        globalOrtho = Shader.GetGlobalFloat(PaperOrthoSizeID);
        globalCamPos = Shader.GetGlobalVector(PaperCamPosID);

        // 读材质（如果你用material.Set...）
        if (targetMaterial)
        {
            matOrtho = targetMaterial.GetFloat(PaperOrthoSizeID);
            matCamPos = targetMaterial.GetVector(PaperCamPosID);
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 520, 200), GUI.skin.box);
        GUILayout.Label($"[CAM] posXY=({camPosXY.x:F3}, {camPosXY.y:F3}) ortho={orthoSize:F3}");

        GUILayout.Label($"[GLOBAL] _PaperCamPos=({globalCamPos.x:F3}, {globalCamPos.y:F3}) _PaperOrthoSize={globalOrtho:F3}");

        if (targetMaterial)
            GUILayout.Label($"[MATERIAL] _PaperCamPos=({matCamPos.x:F3}, {matCamPos.y:F3}) _PaperOrthoSize={matOrtho:F3}");
        else
            GUILayout.Label("[MATERIAL] (not assigned)");

        GUILayout.EndArea();
    }
}