using UnityEngine;

[DefaultExecutionOrder(1000)] // 尽量在Cinemachine更新之后
public class PaperCameraParams_ToMaterial : MonoBehaviour
{
    [Header("Assign the SAME material used by Full Screen Pass Renderer Feature")]
    public Material targetMaterial;

    static readonly int PaperOrthoSizeID = Shader.PropertyToID("_PaperOrthoSize");
    static readonly int PaperCamPosID    = Shader.PropertyToID("_PaperCamPos");

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
    }

    // OnPreRender 在相机真正渲染前调用，基本保证拿到 Cinemachine 最终的相机位置
    void OnPreRender()
    {
        if (!targetMaterial || !cam || !cam.orthographic) return;

        targetMaterial.SetFloat(PaperOrthoSizeID, cam.orthographicSize);

        Vector3 p = cam.transform.position;
        targetMaterial.SetVector(PaperCamPosID, new Vector4(p.x, p.y, 0f, 0f));
    }
}