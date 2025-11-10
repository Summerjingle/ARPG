using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHeathBar : MonoBehaviour
{
    public TextMeshProUGUI myName;
    [Header("血条设置")]
    public Image healthBarFill;
    public Image healthBarBG;

    
    private HealthSystem healthSystem;

    [Header("位置偏移")]
    public Vector3 positionOffset = new Vector3(0, 2f, 0);
    public bool faceCamera = true;

    [Header("高级设置")]
    public bool hideWhenFull = true;
    public float showDurationAfterHit = 3f;
    private float hideTimer;
    private Canvas canvas;
    private Camera mainCamera;

    void Start()
    {
        // 自动获取组件
        canvas = GetComponent<Canvas>();
        mainCamera = Camera.main;

        // 获取 HealthSystem
        healthSystem = GetComponentInParent<HealthSystem>();
        if (healthSystem == null )
        {
            Debug.Log("没找到health system");
        }

        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthChanged; // 直接订阅
            healthSystem.OnDeath += OnFighterDeath;
            healthSystem.OnDeathComplete += OnFighterDeathComplete;
            RefreshHealthBar();
        }


        // 配置Canvas
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
        }

        healthBarFill.enabled = false;
        healthBarBG.enabled = false;
    }

    void Update()
    {
        // 如果角色已死亡，不再更新位置和旋转
        if (healthSystem != null && healthSystem.IsDead) return;

        // 更新血条位置（跟随角色）
        transform.position = healthSystem.transform.position + positionOffset;
        // 让血条始终面向相机
        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        if (myName != null)
        {
            myName.transform.rotation = mainCamera.transform.rotation;
        }

        if (hideWhenFull && canvas != null && healthSystem != null)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0 && healthSystem.Health >= healthSystem.MaxHealth)
            {
                healthBarFill.enabled = false;
                healthBarBG.enabled = false;
            }
        }
    }

    void OnHealthChanged(HealthSystem hs)
    {
        RefreshHealthBar();
    }

    // 修改2：修改方法签名以匹配 Action<MeleeFighter>
    void OnFighterDeath(HealthSystem healthSystem)
    {
        // 虽然参数可能用不到，但为了匹配事件签名必须接受
        if (canvas != null) canvas.enabled = false;
    }

    // 死亡序列完成时调用（可选）
    void OnFighterDeathComplete(HealthSystem healthSystem)
    {
        // 可以在这里添加额外的清理逻辑
        Debug.Log("Death sequence completed, health bar hidden");
    }

    void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthChanged;
            healthSystem.OnDeath -= OnFighterDeath;
            healthSystem.OnDeathComplete -= OnFighterDeathComplete;
        }
    }

    // 手动更新血条（外部调用）
    public void RefreshHealthBar()
    {
        if (healthSystem != null && healthSystem.IsDead) return;
        if (healthBarFill == null || healthSystem == null) return;


        // 计算血量百分比
        float fillAmount = healthSystem.Health / healthSystem.MaxHealth;
        healthBarFill.fillAmount = fillAmount;

        if (hideWhenFull && canvas != null)
        {
            healthBarFill.enabled = true;
            healthBarBG.enabled = true;
            hideTimer = showDurationAfterHit;
        }
    }
}