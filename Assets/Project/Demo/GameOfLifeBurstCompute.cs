using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class GameOfLifeBurstCompute : MonoBehaviour
{
    [Header("Grid")]
    public int width = 160;
    public int height = 90;

    [Header("Timing")]
    [Range(1, 120)] public int stepsPerSecond = 30;
    public bool run = true;

    [Header("Rendering (Compute Shader)")]
    public ComputeShader displayCompute;
    public Renderer targetRenderer; // Quad 的 Renderer（或你自己改成 RawImage）
    public string shaderTextureName = "_MainTex";

    NativeArray<uint> _current;
    NativeArray<uint> _next;

    ComputeBuffer _cellBuffer;
    RenderTexture _rt;
    int _kernel;

    float _accum;

    // 预留：后期交互写入（统一在 step 前应用）
    readonly List<CellEdit> _pendingEdits = new();

    struct CellEdit
    {
        public int x, y;
        public uint v;
    }

    void Start()
    {
        if (displayCompute == null) throw new Exception("Missing displayCompute");

        int count = width * height;
        _current = new NativeArray<uint>(count, Allocator.Persistent);
        _next = new NativeArray<uint>(count, Allocator.Persistent);

        // 可选：随机初始化
        var rnd = new System.Random(1);
        for (int i = 0; i < count; i++)
            _current[i] = (uint)(rnd.NextDouble() > 0.85 ? 1 : 0);

        // GPU buffer：每个 cell 一个 uint
        _cellBuffer = new ComputeBuffer(count, sizeof(uint)); // StructuredBuffer<uint> 对应
        _kernel = displayCompute.FindKernel("CSMain");

        // RenderTexture：160x90，点采样，后面靠材质拉伸显示
        _rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        _rt.Create();

        if (targetRenderer != null)
        {
            targetRenderer.material.SetTexture(shaderTextureName, _rt);
        }

        // 先画一帧
        RenderToTexture();
    }

    void Update()
    {
        if (!run) { HandleDebugInput(); return; }

        float stepInterval = 1f / Mathf.Max(1, stepsPerSecond);
        _accum += Time.deltaTime;

        // 防止帧率低时积累太多：最多追赶几步（你也可以改成 while 全追赶）
        int maxCatchUp = 4;
        int did = 0;

        while (_accum >= stepInterval && did < maxCatchUp)
        {
            _accum -= stepInterval;
            StepOnce();
            did++;
        }

        RenderToTexture();
        HandleDebugInput();
    }

    void StepOnce()
    {
        // 1) 应用交互编辑（在 job 之前，避免并发写）
        ApplyPendingEdits();

        // 2) Burst Job：current -> next
        var job = new LifeStepJob
        {
            width = width,
            height = height,
            current = _current,
            next = _next
        };

        JobHandle handle = job.Schedule(_next.Length, 128);
        handle.Complete();

        // 3) swap
        (_current, _next) = (_next, _current);
    }

    void RenderToTexture()
    {
        // 上传 cells 到 GPU
        _cellBuffer.SetData(_current); // blittable NativeArray<uint> OK :contentReference[oaicite:2]{index=2}

        displayCompute.SetInt("_Width", width);
        displayCompute.SetInt("_Height", height);
        displayCompute.SetBuffer(_kernel, "_Cells", _cellBuffer);
        displayCompute.SetTexture(_kernel, "Result", _rt);

        int gx = Mathf.CeilToInt(width / 8f);
        int gy = Mathf.CeilToInt(height / 8f);

        displayCompute.Dispatch(_kernel, gx, gy, 1); // ComputeShader.Dispatch :contentReference[oaicite:3]{index=3}
    }

    // ----------------------------
    // 交互预留接口（后期你可接鼠标/触摸/网络/编辑器工具）
    // ----------------------------

    public void EnqueueSetCell(int x, int y, bool alive)
    {
        _pendingEdits.Add(new CellEdit { x = x, y = y, v = alive ? 1u : 0u });
    }

    public void EnqueuePaintRect(int x0, int y0, int x1, int y1, bool alive)
    {
        uint v = alive ? 1u : 0u;
        int minX = Mathf.Min(x0, x1);
        int maxX = Mathf.Max(x0, x1);
        int minY = Mathf.Min(y0, y1);
        int maxY = Mathf.Max(y0, y1);

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                _pendingEdits.Add(new CellEdit { x = x, y = y, v = v });
    }

    void ApplyPendingEdits()
    {
        if (_pendingEdits.Count == 0) return;

        for (int i = 0; i < _pendingEdits.Count; i++)
        {
            var e = _pendingEdits[i];
            if ((uint)e.x >= (uint)width || (uint)e.y >= (uint)height) continue;
            _current[e.x + e.y * width] = e.v;
        }
        _pendingEdits.Clear();
    }

    // 临时：按键测试（你后面删掉）
    void HandleDebugInput()
    {
        // 空格暂停/继续
        if (Input.GetKeyDown(KeyCode.Space)) run = !run;

        // 鼠标左键：随机点一个（你后期替换成屏幕->网格映射）
        if (Input.GetMouseButtonDown(0))
        {
            int x = UnityEngine.Random.Range(0, width);
            int y = UnityEngine.Random.Range(0, height);
            EnqueueSetCell(x, y, true);
        }
    }

    void OnDestroy()
    {
        if (_cellBuffer != null) _cellBuffer.Release();
        if (_rt != null) _rt.Release();
        if (_current.IsCreated) _current.Dispose();
        if (_next.IsCreated) _next.Dispose();
    }

    [BurstCompile]
    struct LifeStepJob : IJobParallelFor
    {
        public int width;
        public int height;

        [ReadOnly] public NativeArray<uint> current;
        [WriteOnly] public NativeArray<uint> next;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;

            int neighbors = 0;

            // 开放边界：越界直接跳过（视为死亡）
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = y + dy;
                if ((uint)ny >= (uint)height) continue;

                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    if ((uint)nx >= (uint)width) continue;

                    neighbors += (int)current[nx + ny * width];
                }
            }

            uint alive = current[index];

            // 标准规则：
            // 活细胞：2/3 邻居存活 -> 活，否则死
            // 死细胞：3 邻居存活 -> 活，否则死
            uint outAlive =
                (alive == 1)
                    ? (uint)((neighbors == 2 || neighbors == 3) ? 1 : 0)
                    : (uint)((neighbors == 3) ? 1 : 0);

            next[index] = outAlive;
        }
    }
}
