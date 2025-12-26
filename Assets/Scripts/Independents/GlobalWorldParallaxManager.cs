using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局世界视差管理器：支持编辑器下拉选择排序图层
/// </summary>
public class GlobalWorldParallaxManager : MonoBehaviour
{
    [Serializable]
    public class LayerSettings
    {
        // 这个字符串将由自定义编辑器通过下拉框赋值
        [Tooltip("排序图层名称")]
        public string SortingLayerName;
        [Tooltip("该层物体的移动振幅（正值同向，负值反向）")]
        public float Amplitude = 5f;
        [Tooltip("该层物体的平滑速度")]
        public float Speed = 5f;
    }

    [Header("驱动配置")]
    public Transform Player;
    public float LevelStartX = 0f;
    public float LevelEndX = 100f;

    [Header("排序图层偏移配置")]
    public List<LayerSettings> LayerConfigs = new List<LayerSettings>();

    // 内部类：用于存储识别到的每一个 Sprite 及其初始信息
    private class TrackedSprite
    {
        public Transform Transform;
        public Vector3 StartPosition;
        public LayerSettings Settings;
    }

    private List<TrackedSprite> _trackedSprites = new List<TrackedSprite>();
    private float _horizontalProgress;

    protected virtual void Start()
    {
        InitializeAndFindSprites();
    }

    /// <summary>
    /// 自动查找场景中所有属于指定图层的 Sprite
    /// </summary>
    public void InitializeAndFindSprites()
    {
        _trackedSprites.Clear();
        SpriteRenderer[] allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        foreach (SpriteRenderer sr in allRenderers)
        {
            // 匹配 SortingLayerName
            LayerSettings config = LayerConfigs.Find(c => c.SortingLayerName == sr.sortingLayerName);
            
            if (config != null)
            {
                _trackedSprites.Add(new TrackedSprite
                {
                    Transform = sr.transform,
                    StartPosition = sr.transform.position,
                    Settings = config
                });
            }
        }
    }

    protected virtual void Update()
    {
        if (Player == null || _trackedSprites.Count == 0) return;

        // 计算进度：从 -0.5 到 0.5
        float range = LevelEndX - LevelStartX;
        if (Mathf.Abs(range) > 0.001f)
        {
            _horizontalProgress = Mathf.Clamp01((Player.position.x - LevelStartX) / range) - 0.5f;
        }

        // 批量移动
        foreach (var item in _trackedSprites)
        {
            if (item.Transform == null) continue;

            float targetX = item.StartPosition.x + (_horizontalProgress * item.Settings.Amplitude);
            float nextX = Mathf.Lerp(item.Transform.position.x, targetX, item.Settings.Speed * Time.deltaTime);

            Vector3 pos = item.Transform.position;
            pos.x = nextX;
            item.Transform.position = pos;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(LevelStartX, -50, 0), new Vector3(LevelStartX, 50, 0));
        Gizmos.DrawLine(new Vector3(LevelEndX, -50, 0), new Vector3(LevelEndX, 50, 0));
    }
}