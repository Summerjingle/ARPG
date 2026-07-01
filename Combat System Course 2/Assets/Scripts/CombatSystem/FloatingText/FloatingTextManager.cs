using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 飘字管理器：监听 HealthSystem.OnHealthChanged，从对象池取 TMP 实例，按曲线驱动动画
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("配置")]
    [SerializeField] private FloatingTextDatabase database;
    [SerializeField] private GameObject textPrefab;       // 含 TMP_Text + CanvasGroup 的预制体
    [SerializeField] private int poolSize = 20;

    [Header("调试")]
    [SerializeField] private bool logHealthChanges = false;

    private Queue<FloatingTextInstance> pool;
    private readonly List<FloatingTextInstance> activeInstances = new List<FloatingTextInstance>();
    private Transform poolRoot;
    private Camera mainCamera;

    // ==================== 生命周期 ====================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        poolRoot = new GameObject("FloatingTextPool").transform;
        poolRoot.SetParent(transform);
        pool = new Queue<FloatingTextInstance>(poolSize);
        PrewarmPool();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        // 注册场景中已有的所有 HealthSystem
        foreach (var hs in FindObjectsOfType<HealthSystem>())
        {
            RegisterHealthSystem(hs);
        }
    }

    private void Update()
    {
        // 活跃飘字始终面向相机
        for (int i = activeInstances.Count - 1; i >= 0; i--)
        {
            if (activeInstances[i] != null && mainCamera != null)
            {
                activeInstances[i].transform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    private void OnDestroy()
    {
        // 注销所有 HealthSystem
        foreach (var hs in FindObjectsOfType<HealthSystem>())
        {
            UnregisterHealthSystem(hs);
        }
        if (Instance == this) Instance = null;
    }

    // ==================== 对象池 ====================

    private void PrewarmPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateAndEnqueue();
        }
    }

    private void CreateAndEnqueue()
    {
        GameObject go = textPrefab != null
            ? Instantiate(textPrefab, poolRoot)
            : CreateDefaultPrefab();
        go.name = $"FloatingText_Pooled_{pool.Count}";
        var instance = go.GetComponent<FloatingTextInstance>();
        if (instance == null)
            instance = go.AddComponent<FloatingTextInstance>();
        instance.Initialize();
        go.SetActive(false);
        pool.Enqueue(instance);
    }

    private GameObject CreateDefaultPrefab()
    {
        // 后备：没有预制体时自动创建
        var go = new GameObject("FloatingText");
        go.transform.SetParent(poolRoot);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        tmp.sortingOrder = 100; // 确保渲染在顶层

        var cg = go.AddComponent<CanvasGroup>();
        var instance = go.AddComponent<FloatingTextInstance>();
        return go;
    }

    private FloatingTextInstance GetFromPool()
    {
        if (pool.Count == 0)
            CreateAndEnqueue();

        var instance = pool.Dequeue();
        instance.gameObject.SetActive(true);
        activeInstances.Add(instance);
        return instance;
    }

    public void ReturnToPool(FloatingTextInstance instance)
    {
        if (instance == null) return;

        instance.StopAnimation();
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(poolRoot);

        activeInstances.Remove(instance);
        pool.Enqueue(instance);
    }

    // ==================== HealthSystem 注册 ====================

    public void RegisterHealthSystem(HealthSystem hs)
    {
        if (hs == null) return;
        hs.OnHealthChanged -= OnHealthChanged;
        hs.OnHealthChanged += OnHealthChanged;
    }

    public void UnregisterHealthSystem(HealthSystem hs)
    {
        if (hs == null) return;
        hs.OnHealthChanged -= OnHealthChanged;
    }

    // ==================== 飘字逻辑 ====================

    private void OnHealthChanged(HealthSystem hs, HealthChangeInfo info)
    {
        if (database == null)
        {
            Debug.LogWarning("[FloatingTextManager] FloatingTextDatabase 未配置！");
            return;
        }

        var config = database.SelectConfig(info);
        if (config == null) return;

        if (logHealthChanges)
        {
            string type = info.delta > 0 ? "恢复" : (info.isCrit ? "暴击" : "伤害");
            Debug.Log($"[FloatingText] {hs.name}: {type} {Mathf.Abs(info.delta):F0}");
        }

        SpawnFloatingText(hs.transform, info, config);
    }

    private void SpawnFloatingText(Transform target, HealthChangeInfo info, FloatingTextConfig config)
    {
        var instance = GetFromPool();

        // 文字内容
        float absValue = Mathf.Abs(info.delta);
        instance.tmpText.text = info.delta > 0
            ? $"+{absValue:F0}"
            : $"{absValue:F0}";

        // 样式
        instance.tmpText.color = config.textColor;
        instance.tmpText.fontSize = config.baseFontSize;

        // 图标
        if (instance.iconRenderer != null)
        {
            instance.iconRenderer.sprite = config.icon;
            instance.iconRenderer.enabled = config.icon != null;
        }

        // 世界坐标：目标头顶 + 随机水平偏移
        Vector3 randomOffset = new Vector3(
            Random.Range(-config.randomHorizontalRange, config.randomHorizontalRange),
            config.heightOffset,
            0f
        );
        instance.transform.position = target.position + randomOffset;

        // 启动动画
        instance.PlayAnimation(config, () => ReturnToPool(instance));
    }
}
